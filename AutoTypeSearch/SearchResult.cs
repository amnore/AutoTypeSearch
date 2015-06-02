using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using KeePassLib;

namespace AutoTypeSearch
{
	internal class SearchResult
	{
		private readonly PwDatabase mDatabase;
		private readonly PwEntry mEntry;
		private readonly string mFieldName;
		private readonly int mStart;
		private readonly int mLength;
		private readonly string mFieldValue;
		private readonly string mTitle;
		private string mUniqueTitlePart;
		private int mResultIndex = -1;

		public SearchResult(PwDatabase database, PwEntry entry, string title, string fieldName, string fieldValue, int start, int length)
		{
			mDatabase = database;
			mEntry = entry;
			mFieldName = fieldName;
			mFieldValue = fieldValue;
			mStart = start;
			mLength = length;
			mTitle = title;

			Debug.Assert(mLength >= 0 && mStart >= 0, "Negative values are invalid");
			Debug.Assert(mLength > 0 || mStart == 0, "Length must be non-zero (unless no highlight)");
			Debug.Assert((mStart + mLength) <= fieldValue.Length, "Length out of range");
		}

		public PwDatabase Database
		{
			get { return mDatabase; }
		}

		public PwEntry Entry
		{
			get { return mEntry; }
		}

		public string FieldName
		{
			get { return mFieldName; }
		}

		public string FieldValue
		{
			get { return mFieldValue; }
		}

		public int Start
		{
			get { return mStart; }
		}

		public int Length
		{
			get { return mLength; }
		}

		public string Title
		{
			get { return mTitle; }
		}

		/// <summary>
		/// The UniqueTitle may be modified from the <seealso cref="Title"/> to ensure uniqueness in the list of results
		/// </summary>
		public string UniqueTitle
		{
			get { return UniqueTitlePart + Title; }
		}

		public string UniqueTitlePart
		{
			get { return mUniqueTitlePart; }
		}

		public int ResultIndex
		{
			get { return mResultIndex; }
		}

		public void SetResultIndex(int resultIndex)
		{
			if (mResultIndex != -1)
			{
				throw new InvalidOperationException("Result index has already been set");
			}
			if (resultIndex < 0)
			{
				throw new ArgumentOutOfRangeException("resultIndex");
			}

			mResultIndex = resultIndex;
		}

		/// <summary>
		/// Sets <see cref="UniqueTitle"/> by including parent group names to the specified depth.
		/// </summary>
		/// <returns>True if the group hierarchy is deep enough to support full requested <paramref name="depth"/></returns>
		public bool SetUniqueTitleDepth(int depth)
		{
			var groupPath = new StringBuilder();
			var group = Entry.ParentGroup;
			for (int i = 0; i < depth && group != null; i++)
			{
				groupPath.Insert(0, group.Name + " / ");
				group = group.ParentGroup;
			}

			mUniqueTitlePart = groupPath.ToString();

			return group != null;
		}

		
	}
}
