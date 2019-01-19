using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using AutoTypeSearch.Properties;
using KeePass.Util.Spr;
using KeePassLib;
using KeePassLib.Utility;

namespace AutoTypeSearch
{
	internal class SearchResults
	{
		private readonly string mTerm;
		private readonly SearchResult[] mResults;

		private readonly object mLock = new object();
		private volatile int mCount;
		private volatile bool mComplete;

		private readonly AutoResetEvent mResultsUpdated = new AutoResetEvent(false);

		private readonly CompareOptions mStringComparison;
		private readonly bool mSearchTitle;
		private readonly bool mSearchUserName;
		private readonly bool mSearchUrl;
		private readonly bool mSearchNotes;
		private readonly bool mSearchCustomFields;
		private readonly bool mResolveReferences;
		private readonly bool mSearchTags;

		public SearchResults(int capacity, string term)
		{
			mTerm = term;
			mResults = new SearchResult[capacity];

			mStringComparison = Settings.Default.CaseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase;
			mStringComparison |= CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreNonSpace;
			mSearchTitle = Settings.Default.SearchTitle;
			mSearchUserName = Settings.Default.SearchUserName;
			mSearchUrl = Settings.Default.SearchUrl;
			mSearchNotes = Settings.Default.SearchNotes;
			mSearchCustomFields = Settings.Default.SearchCustomFields;
			mSearchTags = Settings.Default.SearchTags;
			mResolveReferences = Settings.Default.ResolveReferences;
		}

		/// <summary>
		/// Gets an ordered list of fields to search for the term
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		private IEnumerable<string> GetFieldsToSearch(PwEntry entry)
		{
			var fieldsToSearch = new List<String>((int)entry.Strings.UCount);
			if (mSearchTitle) fieldsToSearch.Add(PwDefs.TitleField);
			if (mSearchUserName) fieldsToSearch.Add(PwDefs.UserNameField);
			if (mSearchUrl) fieldsToSearch.Add(PwDefs.UrlField);
			if (mSearchNotes) fieldsToSearch.Add(PwDefs.NotesField);
			if (mSearchCustomFields)
			{
				foreach (var stringEntry in entry.Strings)
				{
					if (!stringEntry.Value.IsProtected && !PwDefs.IsStandardField(stringEntry.Key))
					{
						fieldsToSearch.Add(stringEntry.Key);
					}
				}
			}
			if (mSearchTags) fieldsToSearch.Add(AutoTypeSearchExt.TagsVirtualFieldName);

			return fieldsToSearch;
		}

		public void AddResultIfMatchesTerm(PwDatabase context, PwEntry entry)
		{
			// First try without resolving
			var addedResult = AddResultIfMatchesTerm(context, entry, false);

			if (!addedResult && mResolveReferences)
			{
				// Not found without resolving, so try resolving
				AddResultIfMatchesTerm(context, entry, true);
			}
		}

		private bool AddResultIfMatchesTerm(PwDatabase context, PwEntry entry, bool resolveReferences)
		{
			foreach (var fieldName in GetFieldsToSearch(entry))
			{
				string fieldValue;
				if (fieldName == AutoTypeSearchExt.TagsVirtualFieldName)
				{
					fieldValue = StrUtil.TagsToString(entry.Tags, true);
				}
				else
				{
					fieldValue = entry.Strings.ReadSafeEx(fieldName);

					if (resolveReferences)
					{
						fieldValue = ResolveReferences(context, entry, fieldValue);
					}
				}

				if (!String.IsNullOrEmpty(fieldValue))
				{
					var foundIndex = CultureInfo.CurrentCulture.CompareInfo.IndexOf(fieldValue, mTerm, mStringComparison);
					if (foundIndex >= 0)
					{
						// Found a match, create a search result and add it
						AddResult(new SearchResult(context, entry, entry.Strings.ReadSafe(PwDefs.TitleField), fieldName, fieldValue, foundIndex, mTerm.Length));
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Resolves any references in the field value and returns it. If there were no references,
		/// returns null (to avoid duplicate searching - it is assumed that the unresolved value has already been searched)
		/// </summary>
		private string ResolveReferences(PwDatabase context, PwEntry entry, string fieldValue)
		{
			if (fieldValue.IndexOf('{') < 0)
			{
				// Can't contain any references
				return null;
			}
			
			var sprContext = new SprContext(entry, context, SprCompileFlags.Deref) { ForcePlainTextPasswords = false };

			var result = SprEngine.Compile(fieldValue, sprContext);
			if (CultureInfo.CurrentCulture.CompareInfo.Compare(result,fieldValue, mStringComparison) == 0)
			{
				return null;
			}
			
			return result;
		}

		public void AddResultIfMatchesTerm(SearchResult candidate)
		{
			// First see whether the existing candidate is a further match in the same place
			var fieldValue = candidate.FieldValue;
			if (fieldValue.Length > candidate.Start + mTerm.Length && CultureInfo.CurrentCulture.CompareInfo.Compare(fieldValue.Substring(candidate.Start, mTerm.Length), mTerm, mStringComparison) == 0)
			{
				// Yep, match continues, so add it.
				AddResult(new SearchResult(candidate.Database, candidate.Entry, candidate.Title, candidate.FieldName, fieldValue, candidate.Start, mTerm.Length));
			}
			else
			{
				// Existing candidate match couldn't be extended, so search from scratch again
				AddResultIfMatchesTerm(candidate.Database, candidate.Entry);
			}
		}

		private void AddResult(SearchResult result)
		{
			lock (mLock)
			{
				if (mComplete)
				{
					throw new InvalidOperationException("Search results have been completed");
				}
				result.SetResultIndex(mCount);
				mResults[mCount++] = result;
			}
			mResultsUpdated.Set();
		}

		/// <summary>
		/// Indicates that the results are complete, and no more will be added.
		/// </summary>
		public void SetComplete()
		{
			lock (mLock)
			{
				mComplete = true;
			}
			mResultsUpdated.Set();
		}

		/// <summary>
		/// Gets all the available results so far.
		/// </summary>
		/// <param name="index">Index to start returning from. Modified to be the first index not available yet on return.</param>
		/// <param name="complete">Set to true if the results are complete, false if more results are pending but have not been returned.</param>
		/// <returns></returns>
		public SearchResult[] GetAvailableResults(ref int index, out bool complete)
		{
			int count;
			lock (mLock)
			{
				count = mCount;
				complete = mComplete;
			}

			if (count <= index)
			{
				return new SearchResult[0];
			}

			var availableResults = new SearchResult[count - index];
			Array.Copy(mResults, index, availableResults, 0, availableResults.Length);
			index = count;

			return availableResults;
		}

		/// <summary>
		/// Gets all the results. Will block until complete.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<SearchResult> GetAllResults()
		{
			int count = -1;

			for (var i = 0; i < mResults.Length; i++)
			{
				if (i > count)
				{
					// Reached the limit of availability so far, so see if more is available
					do
					{
						bool moreAvailable, complete;

						lock (mLock)
						{
							moreAvailable = mCount > count;
							count = mCount;
							complete = mComplete;
						}

						if (!moreAvailable)
						{
							if (complete)
							{
								// No more available, but the results are now complete anyway
								yield break;
							}

							// No more available yet, not yet complete, wait until more becomes available
							mResultsUpdated.WaitOne();
						}
						else
						{
							// More available now, so stop checking for more, continue with the loop to return them
							break;
						}
					} while (true);

					Debug.Assert(i <= count, "More should be available now");
				}

				yield return mResults[i];
			}
		}

		public SearchResults CreateChildResults(string term)
		{
			Debug.Assert(term.StartsWith(mTerm));

			int count;
			bool complete;
			lock (mLock)
			{
				count = mCount;
				complete = mComplete;
			}

			// If complete, then we know we don't need more than count. Otherwise, it can't be more than this capacity anyway
			var childCapacity = complete ? count : mResults.Length;

			return new SearchResults(childCapacity, term);
		}
	}
}
