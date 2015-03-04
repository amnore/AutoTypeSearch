using System;
using System.Diagnostics;
using System.Linq;
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
	}
}
