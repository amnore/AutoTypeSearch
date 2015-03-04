using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoTypeSearch.Properties;
using KeePassLib;

namespace AutoTypeSearch
{
	internal class Searcher
	{
		private readonly PwDatabase[] mDatabases;
		private readonly Dictionary<string, SearchResults> mSearches = new Dictionary<string, SearchResults>();

		public Searcher(PwDatabase[] databases)
		{
			mDatabases = databases;
		}

		public SearchResults Search(string term)
		{
			if (term.Length < 2)
			{
				throw new ArgumentException("Search term must be at least 2 characters");
			}

			SearchResults parentResults = null;

			var termParent = term;
			while (termParent.Length >= 2)
			{
				if (mSearches.TryGetValue(termParent, out parentResults))
				{
					if (termParent == term)
					{
						// This is an exact duplicate search, so return it.
						return parentResults;
					}

					// Found an existing search for a parent of the term, start from there.
					break;
				}
		
				// No existing search for termParent found, try less.
				termParent = termParent.Remove(termParent.Length - 1, 1);
			}

			SearchResults searchResults;
			if (parentResults == null)
			{
				// No parent found at all, start from scratch
				searchResults = new SearchResults(GetCountOfAllDatabaseEntries(), term);

				var rootSearchThread = new Thread(RootSearchWorker) { Name = term };
				rootSearchThread.Start(searchResults);
			}
			else
			{
				searchResults = parentResults.CreateChildResults(term);

				var childSearchThread = new Thread(ChildSearchWorker) { Name = term };
				childSearchThread.Start(new ChildSearchWorkerState{ Source = parentResults, Results = searchResults });
			}

			mSearches.Add(term, searchResults);

			return searchResults;
		}

		private int GetCountOfAllDatabaseEntries()
		{
			return (from database in mDatabases select (int)database.RootGroup.GetEntriesCount(true)).Sum();
		}

		private void RootSearchWorker(object stateObject)
		{
			var results = (SearchResults)stateObject;
			var excludeExpired = Settings.Default.ExcludeExpired;
			var searchStartTime = DateTime.Now;

			foreach (var database in mDatabases)
			{
				SearchGroup(database, database.RootGroup, results, excludeExpired, searchStartTime);	
			}

			results.SetComplete();
		}

		/// <summary>
		/// Recursively search <paramref name="group"/> and its children, adding results to <paramref name="results"/>
		/// </summary>
		private void SearchGroup(PwDatabase context, PwGroup group, SearchResults results, bool excludeExpired, DateTime searchStartTime)
		{
			if (group.EnableSearching ?? true) // Group will only be searched if it's parent enabled searching, so if it is inherit (null) or true, search it.
			{
				foreach (var childGroup in group.Groups)
				{
					SearchGroup(context, childGroup, results, excludeExpired, searchStartTime);
				}

				foreach (var entry in group.Entries)
				{
					if (!(excludeExpired && entry.Expires && searchStartTime > entry.ExpiryTime))
					{
						results.AddResultIfMatchesTerm(context, entry);
					}
				}
			}
		}

		private struct ChildSearchWorkerState
		{
			public SearchResults Source;
			public SearchResults Results;
		}
		private void ChildSearchWorker(object stateObject)
		{
			var state = (ChildSearchWorkerState)stateObject;

			bool complete;
			var index = 0;
			do
			{
				foreach (var entry in state.Source.GetAvailableResults(ref index, out complete))
				{
					state.Results.AddResultIfMatchesTerm(entry);
				}
			} while (!complete);

			state.Results.SetComplete();
		}
	}
}
