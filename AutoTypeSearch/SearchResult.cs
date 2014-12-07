using System;
using System.Diagnostics;
using System.Linq;
using KeePassLib;

namespace AutoTypeSearch
{
	internal class SearchResult
	{
		private readonly PwEntry mEntry;
		private readonly string mFieldName;
		private readonly int mStart;
		private readonly int mLength;
		private readonly string mFieldValue;

		public SearchResult(PwEntry entry, string fieldName, string fieldValue, int start, int length)
		{
			mEntry = entry;
			mFieldName = fieldName;
			mFieldValue = fieldValue;
			mStart = start;
			mLength = length;

			Debug.Assert(mLength >= 0 && mStart >= 0, "Negative values are invalid");
			Debug.Assert(mLength > 0 || mStart == 0, "Length must be non-zero (unless no highlight)");
			Debug.Assert((mStart + mLength) <= fieldValue.Length, "Length out of range");
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
	}
}
