using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace qqEd
{
    // StrDelta handles the result of the comparison between two strings (string-comparison).
    // The result of the comparison is represented by a sequence of modifications (or 'edits') (see DItem).
    // Comments below only refer to the case of 'string-comparison', 
    // but StrDelta applies to int-array-comparison and string-list-comparison as well. 
    // The comparison has a 'direction': there are original and target strings ('from' and 'to'), 
    // and the differences are intended as "modifications to apply to 'from' in order to get 'to'".
    public class StrDelta
    {
        public string from = null;          // (valid only in case of string-comparison)
        public string to = null;            // (valid only in case of string-comparison)

        public List<string> fromList = null;    // (valid only in case of string-list-comparison)
        public List<string> toList = null;      // (valid only in case of string-list-comparison)

        public int distance = -1;               // Number of atomic modifications (add/remove/modify of one character) to modify 'from' to 'to'

        public bool conflict = false;       // True in case of merge conflict

        // A DItem (Delta Item) represents a single homogeneous difference between two string (string-comparison).
        // A DItem affects contiguous characters and it's of kind 'add', 'remove' or 'replace'.
        // A generic difference between two strings is represented by a sequence of DItem.
        public class DItem
        {
            public bool add = false;    // True if DItem is of kind 'add'. 
            public bool rem = false;    // True if DItem is of kind 'remove'.
            // If 'add' and 'rem' are both 'true', then DItem is of kind 'replace'.

            public int pos = 0;         // Position of the edit in the 'from' string
            public int width = 0;       // Width of the edit in the 'from' string
            public int widthOtherSide = 0;  // Needed by the 'keepAligned' feature: one side has to know the width of the modification on the other side.
            public string data_str = null;   // (valid only in case of string-comparison)
            public List<string> data_strList = null;   // (valid only in case of string-list-comparison)

            public int pos_r = 0;       // Position of the edit in the 'to' string
            public int width_r = 0;     // Width of the edit in the 'to' string
            public int widthOtherSide_r = 0;  // Needed by the 'keepAligned' feature: one side has to know the width of the modification on the other side.
            public string data_str_r = null;   // (valid only in case of string-comparison)
            public List<string> data_strList_r = null;   // (valid only in case of string-list-comparison)

            public DItem(bool add, bool rem, int pos, int width, int pos_r, int width_r)
            {
                this.add = add;
                this.rem = rem;
                this.pos = pos;
                this.width = width;

                this.pos_r = pos_r;
                this.width_r = width_r;
            }

            // Copy constructor
            public DItem(DItem ed)
            {
                this.add = ed.add;
                this.rem = ed.rem;
                this.pos = ed.pos;
                this.width = ed.width;
                this.widthOtherSide = ed.widthOtherSide;
                this.data_str = ed.data_str;
                if (ed.data_strList != null)
                {
                    this.data_strList = new List<string>(ed.data_strList);
                }
                this.pos_r = ed.pos_r;
                this.width_r = ed.width_r;
                this.widthOtherSide_r = ed.widthOtherSide_r;
                this.data_str_r = ed.data_str_r;
                if (ed.data_strList_r != null)
                {
                    this.data_strList_r = new List<string>(ed.data_strList_r);
                }
            }

            public bool IsEqual(DItem ed)
            {
                
                return ((this.add == ed.add)
                    && (this.rem == ed.rem)
                    && (this.pos == ed.pos)
                    && (this.width == ed.width)
                    && (this.widthOtherSide == ed.widthOtherSide)
                    && (this.data_str == ed.data_str)
                    && (this.data_strList.GetHashCode() == ed.data_strList.GetHashCode())   // todo: are you sure you can compare lists this way?
                    && (this.pos_r == ed.pos_r)
                    && (this.width_r == ed.width_r)
                    && (this.widthOtherSide_r == ed.widthOtherSide_r)
                    && (this.data_str_r == ed.data_str_r)
                    && (this.data_strList_r.GetHashCode() == ed.data_strList_r.GetHashCode()));
            }
        };

        public List<DItem> edits;        // Sequence of modifications to be applied to the string 'from' to get 'to'

        public StrDelta()
        {
        }

        // Contructor in case of string-comparison
        public StrDelta(string sF, string sT)
        {
            this.from = sF;
            this.to = sT;
        }

        // Contructor in case of string-list-comparison
        public StrDelta(List<string> fL, List<string> tL)
        {
            this.fromList = new List<string>(fL);
            this.toList = new List<string>(tL);
        }

        // Copy constructor
        public StrDelta(StrDelta dlt)
        {
            this.from = dlt.from;
            this.to = dlt.to;
            this.fromList = dlt.fromList;
            this.toList = dlt.toList;
            this.distance = dlt.distance;
            this.conflict = dlt.conflict;

            this.edits = new List<DItem>();
            foreach (DItem ed in dlt.edits)
            {
                this.edits.Add(new DItem(ed));
            }
        }

        // Applies this.edits to this.from to calculate this.to
        public void ApplyEdits()
        {
            if (IsStringComparison())
            {
                ApplyEdits_str();
            }
            else
            {
                ApplyEdits_strList();
            }
        }

        // This function implements ApplyEdits() in case of string-comparison
        private void ApplyEdits_str()
        {
            if (this.conflict)
            {
                this.to = "<conflict>";
                return;
            }
            StringBuilder sb = new StringBuilder();
            int currPosF = 0;    // Current position in the 'from' string
            for (int i = 0; i < this.edits.Count; i++)
            {
                DItem edF = this.edits[i];
                if (edF.pos > currPosF)
                {
                    // Subrange with no edits
                    sb.Append(this.from.Substring(currPosF, edF.pos - currPosF));
                }

                currPosF = edF.pos;

                if ((edF.add == true) && (edF.rem == false))
                {
                    sb.Append(edF.data_str_r);
                }
                else if ((edF.add == false) && (edF.rem == true))
                {
                    currPosF += edF.width;
                }
                else if ((edF.add == true) && (edF.rem == true))
                {
                    sb.Append(edF.data_str_r);
                    currPosF += edF.width;
                }
            }
            // Last part (with no edits)
            sb.Append(this.from.Substring(currPosF, this.from.Length - currPosF));     
            this.to = sb.ToString();
        }

        // This function implements ApplyEdits() in case of string-list-comparison
        // Given 'fromList' and 'edits', this function re-applies the edits to re-calculate 'toList'.
        private void ApplyEdits_strList()
        {
            this.toList = new List<string>();
            if (this.conflict)
            {
                this.toList.Add("<conflict>");
                return;
            }
            int currPosF = 0;    // Current position in the 'from' string
            for (int i = 0; i < this.edits.Count; i++)
            {
                DItem edF = this.edits[i];
                if (edF.pos > currPosF)
                {
                    // Subrange with no edits
                    this.toList.AddRange(this.fromList.GetRange(currPosF, edF.pos - currPosF));
                }

                currPosF = edF.pos;

                if ((edF.add == true) && (edF.rem == false))
                {
                    this.toList.AddRange(edF.data_strList_r);
                }
                else if ((edF.add == false) && (edF.rem == true))
                {
                    currPosF += edF.width;
                }
                else if ((edF.add == true) && (edF.rem == true))
                {
                    this.toList.AddRange(edF.data_strList_r);
                    currPosF += edF.width;
                }
            }
            // Last part (with no edits)
            this.toList.AddRange(this.fromList.GetRange(currPosF, this.fromList.Count - currPosF));
        }

        // Given a list of edits, calculate for each edit the affected text.
        // Assign field 'data_string' for each edit.
        private void _CalculateEditData()
        {
            if (IsStringComparison())
            {
                _CalculateEditData_str();
            }
            else
            {
                _CalculateEditData_strList();
            }
        }

        private void _CalculateEditData_str()
        {
            if (this.edits.Count == 0)
            {
                return;
            }
            int currPos = 0;
            int currPos_r = 0;
            int i = 0;
            foreach (DItem ed in this.edits)
            {
                currPos = ed.pos;
                if (ed.rem)
                {
                    this.edits[i].data_str = this.from.Substring(ed.pos, ed.width);
                    currPos += ed.width;
                }
                currPos_r = ed.pos_r;
                if (ed.add)
                {
                    this.edits[i].data_str_r = this.to.Substring(ed.pos_r, ed.width_r);
                    currPos_r += ed.width_r;
                }
                i++;
            }
        }

        private void _CalculateEditData_strList()
        {
            if (this.edits.Count == 0)
            {
                return;
            }
            int currPos = 0;
            int currPos_r = 0;
            int i = 0;
            foreach (DItem ed in this.edits)
            {
                this.edits[i].data_strList = new List<string>();
                this.edits[i].data_strList_r = new List<string>();
                currPos = ed.pos;
                if (ed.rem)
                {
                    this.edits[i].data_strList.AddRange(this.fromList.GetRange(ed.pos, ed.width));
                    currPos += ed.width;
                }
                currPos_r = ed.pos_r;
                if (ed.add)
                {
                    this.edits[i].data_strList_r.AddRange(this.toList.GetRange(ed.pos_r, ed.width_r));
                    currPos_r += ed.width_r;
                }
                i++;
            }
        }

        // Given a list of 'edits' of size 1, this function groups them when they are contiguous.
        // e.g. 2 contiguous edits of size 1 => 1 edit of size 2.
        // This is a low-level private function (i.e. less readability, weaker error detection, ...)
        // Precondition: assume that DItem.width is always 0 or 1.
        private void _Compact()
        {
            List<DItem> edits_new = new List<DItem>();
            DItem grouped;
            if (this.edits.Count == 0)
            {
                return;
            }
            grouped = new DItem(this.edits[0]);   // Initialize the result by putting the first edit
            grouped.width = 0;
            grouped.width_r = 0;
            foreach (DItem m in this.edits)
            {
                if ((m.add == grouped.add) && (m.rem == grouped.rem))
                {
                    if ((m.pos == grouped.pos)    // This is true only for the first loop
                        || (m.pos == (grouped.pos + grouped.width)))   // If same group as before ...
                    {
                        grouped.width += m.width;
                        grouped.width_r += m.width_r;
                    }
                    else    // If same kind of 'edit', but with a hole ...
                    {
                        edits_new.Add(grouped);
                        grouped = new DItem(m);    // ... start a new group
                    }
                }
                else       // If a different kind of 'edit' ...
                {
                    edits_new.Add(grouped);
                    grouped = new DItem(m);        // ... start a new group
                }
            }
            edits_new.Add(grouped);

            this.edits = edits_new;
        }

        // Compare 'from' and 'to' to calculate 'edits' (of 'fromList' and 'toList') in case of string-list-comparison.
        public int Compare()
        {
            if (IsStringComparison())
            {
                return Compare_str();
            }
            else
            {
                return Compare_strList();
            }
            return int.MaxValue;
        }

        // Compare two strings (string-comparison).
        // Given 'from' and 'to', this function calculates the result of the comparison 'edits'.
        // This function uses the Levenshtein algorithm.
        private int Compare_str()
        {
            this.edits = new List<DItem>();     // Sequence of modifications to apply to 'from'

            if (this.from == null || this.to == null)
            {
                return int.MaxValue;
            }

            int lF = this.from.Length;
            int lT = this.to.Length;

            if ((lF > 0) && (lT == 0))
            {
                this.edits.Add(new DItem(false, true, 0, lF, 0, lF));
                this.edits[0].data_str = this.from;
                this.edits[0].widthOtherSide = lF;
                this.edits[0].data_str_r = this.to;
                this.edits[0].widthOtherSide_r = lF;
                this.distance = lF;
                return this.distance;
            }
            if ((lF == 0) && (lT > 0))
            {
                this.edits.Add(new DItem(true, false, 0, lT, 0, lT));
                this.edits[0].data_str = this.from;
                this.edits[0].widthOtherSide = lF;
                this.edits[0].data_str_r = this.to;
                this.edits[0].widthOtherSide_r = lF;
                this.distance = lT;
                return this.distance;
            }

            // Algorithm from https://stackoverflow.com/questions/9453731/how-to-calculate-distance-similarity-measure-of-given-2-strings
            // See also https://www.dotnetperls.com/levenshtein
            var distances = new int[lF + 1, lT + 1];
            for (int i = 0; i <= lF; distances[i, 0] = i++) ;
            for (int j = 0; j <= lT; distances[0, j] = j++) ;

            for (int i = 1; i <= lF; i++)
            {
                for (int j = 1; j <= lT; j++)
                {
                    int cost = this.to[j - 1] == this.from[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }

            // Print matrix (only for debug)
            // Debug.Print("");
            // for (int i = 0; i <= lF; i++)
            // {
            //     string s = "";
            //     for (int j = 0; j <= lT; j++)
            //     {
            //         s += distances[i,j].ToString() + " ";
            //     }
            //     Debug.Print(s);
            // }

            // Extract the list of single-character edits
            // Debug.Print(distances[lengthA, lengthB].ToString());
            {
                int i = lF;
                int j = lT;
                while (true)
                {
                    int c = distances[i, j];
                    if (c == 0)
                    {
                        break;
                    }
                    int l = c;
                    int u = c;
                    int d = c;
                    if (j > 0)
                    {
                        l = distances[i, j - 1];
                    }
                    if (i > 0)
                    {
                        u = distances[i - 1, j];
                    }
                    if ((i > 0) && (j > 0))
                    {
                        d = distances[i - 1, j - 1];
                    }
                    if ((d <= l) && (d <= u))   // Diag direction = MOD
                    {
                        if ((c - d) > 0)
                        {
                            this.edits.Add(new DItem(true, true, i - 1, 1, j - 1, 1));
                        }
                        i--;
                        j--;
                    }
                    else if (l < u)   // LEFT direction = ADD
                    {
                        this.edits.Add(new DItem(true, false, i, 0, j - 1, 1));
                        j--;
                    }
                    else // u<l = UP direction = REM
                    {
                        this.edits.Add(new DItem(false, true, i - 1, 1, j, 0));
                        i--;
                    }
                }
            }

            this.edits.Reverse();

            // Now 'this.edits' has only modifications os size 1 (i.e. only one character of the string is added/deleted/modified) 
            // => they have to be grouped
            _Compact();

            _CalculateEditData();     // Assign field 'data_string' for each edit.

            _CalculateWidthOtherSide();

            this.distance = distances[lF, lT];

            return this.distance;
        }

        // Compare two list of strings (string-list-comparison).
        // Given 'fromList' and 'toList', this function calculates the result of the comparison 'edits'.
        // The function calculates a hash-code for each list-item, then the function compares the two arrays of hash-codes (int-array-comparison).
        // Notice: Compare_str() and Compare_strList() are very similar => todo: unify them.
        private int Compare_strList()
        {
            this.edits = new List<DItem>();     // Sequence of modifications to apply to 'fromList'

            int[] sF = Util.Hash_StringList2DigestArray(this.fromList, false);
            int[] sT = Util.Hash_StringList2DigestArray(this.toList, false);

            int lF = sF.Length;
            int lT = sT.Length;

            if ((lF == 0) || (lT == 0))
            {
                if (lF > 0)
                {
                    this.edits.Add(new DItem(false, true, 0, lF, 0, lF));
                    this.edits[0].widthOtherSide = lF;
                    this.edits[0].widthOtherSide_r = lF;
                }
                if (lT > 0)
                {
                    this.edits.Add(new DItem(true, false, 0, lT, 0, lT));
                    this.edits[0].widthOtherSide = lF;
                    this.edits[0].widthOtherSide_r = lF;
                }
                this.distance = Math.Max(lF, lT);
                return this.distance;
            }

            var distances = new int[lF + 1, lT + 1];
            for (int i = 0; i <= lF; distances[i, 0] = i++) ;
            for (int j = 0; j <= lT; distances[0, j] = j++) ;

            for (int i = 1; i <= lF; i++)
            {
                for (int j = 1; j <= lT; j++)
                {
                    int cost = sT[j - 1] == sF[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }

            // Print matrix (only for debug)
            // Debug.Print("");
            // for (int i = 0; i <= lF; i++)
            // {
            //     string s = "";
            //     for (int j = 0; j <= lT; j++)
            //     {
            //         s += distances[i,j].ToString() + " ";
            //     }
            //     Debug.Print(s);
            // }

            // Extract the list of single-character edits
            // Debug.Print(distances[lengthA, lengthB].ToString());
            {
                int i = lF;
                int j = lT;
                while (true)
                {
                    int c = distances[i, j];
                    if (c == 0)
                    {
                        break;
                    }
                    int l = c;
                    int u = c;
                    int d = c;
                    if (j > 0)
                    {
                        l = distances[i, j - 1];
                    }
                    if (i > 0)
                    {
                        u = distances[i - 1, j];
                    }
                    if ((i > 0) && (j > 0))
                    {
                        d = distances[i - 1, j - 1];
                    }
                    if ((d <= l) && (d <= u))   // Diag direction = MOD
                    {
                        if ((c - d) > 0)
                        {
                            this.edits.Add(new DItem(true, true, i - 1, 1, j - 1, 1));
                        }
                        i--;
                        j--;
                    }
                    else if (l < u)   // LEFT direction = ADD
                    {
                        this.edits.Add(new DItem(true, false, i, 0, j - 1, 1));
                        j--;
                    }
                    else // u<l = UP direction = REM
                    {
                        this.edits.Add(new DItem(false, true, i - 1, 1, j, 0));
                        i--;
                    }
                }
            }

            this.edits.Reverse();

            // Now 'this.edits' has only modifications os size 1 (i.e. only one character of the string is added/deleted/modified) 
            // => they have to be grouped
            _Compact();

            _CalculateEditData();     // Assign field 'data_string' for each edit.

            _CalculateWidthOtherSide();

            this.distance = distances[lF, lT];

            return this.distance;
        }

        // 0 = values_are_identical
        // 1 = value 1 is smaller
        // 2 = value 2 is smaller
        private int _IntCompare(int x1, int x2)
        {
            if (x1 < x2)
            {
                return 1;
            }
            else if (x2 < x1)
            {
                return 2;
            }
            return 0;
        }

        public bool IsStringComparison()
        {
            return (from != null);
        }

        // Join two StrDelta-objects i.e. merge the current StrDelta-object with a given StrDelta-object.
        // This function modifies this.edits and this.to.
        // The two StrDelta-objects must have the same this.from i.e. same ancestor.
        // The function returns 'false' if the merge is not feasible or in case of merge conflicts (i.e. two edits to be applied to the same item).
        // Error management: by error code.
        public bool Merge(StrDelta dlt)
        {
            StrDelta _dlt1 = new StrDelta(this);
            StrDelta _dlt2 = new StrDelta(dlt);
            this.edits = new List<DItem>();
            this.conflict = false;

            if (this.from != dlt.from)
            {
                return false;   // Input StrDelta must have the same 'from'
            }

            int i1 = 0;
            int i2 = 0;
            while (true)
            {
                int winner = -1;    // -1=not_assigned; 0=edits_are_identical; 1=chosen_edit_1; 2=chosen_edit_2
                DItem from1 = null;
                DItem from2 = null;
                if (i1 < _dlt1.edits.Count)
                {
                    from1 = _dlt1.edits[i1];
                }
                if (i2 < _dlt2.edits.Count)
                {
                    from2 = _dlt2.edits[i2];
                }
                if ((from1 == null) && (from2 == null))     // If finished ...
                {
                    break;
                }
                if (from1 == null)                          // If 'from1' completely processed ...
                {
                    winner = 2;
                }
                if (from2 == null)                          // If 'from2' completely processed ...
                {
                    winner = 1;
                }

                if (winner == -1)
                {
                    // Justification: _dlt1 and _dlt1 can be compared because they are kept aligned (see code below)
                    // Justification: either 'width' or 'widthOtherSide' is zero
                    if (Util.RangeOverlapping(from1.pos, from1.pos + from1.width + from1.widthOtherSide - 1,
                        from2.pos, from2.pos + from2.width + from2.widthOtherSide - 1))   // If the 2 edits overlap ...
                    {
                        if (!from1.IsEqual(from2))     // ... and they differs ...
                        {
                            this.conflict = true;
                            return false;
                        }
                    }
                    winner = _IntCompare(from1.pos, from2.pos);  // 0=edits_are_identical; 1=chosen_edit_1; 2=chosen_edit_2
                }

                if (winner == 0)            // If the 2 edits are identical ...
                {
                    this.edits.Add(new DItem(from1));

                    i1++;
                    i2++;
                }
                else if (winner == 1)       // If _dlt1 has been selected ...
                {
                    this.edits.Add(new DItem(from1));

                    // Align _dlt2: all 'loser's edits have to be shifted by the width of the 'winner'
                    int d = 0;          // 0 is used in case of 'mod'
                    if ((from1.add == true) && (from1.rem == false))       // If 'add' ...
                    {
                        d = from1.width + from1.widthOtherSide;
                    }
                    if ((from1.add == false) && (from1.rem == true))       // If 'rem' ...
                    {
                        d = -(from1.width + from1.widthOtherSide);
                    }
                    for (int j = i2; j < _dlt2.edits.Count; j++)
                    {
                        _dlt2.edits[j].pos_r += d;    // Align _dlt2
                    }
                    i1++;
                }
                else if (winner == 2)         // If _dlt2 has been selected ...
                {
                    this.edits.Add(new DItem(from2));

                    // Align _dlt1: all 'loser's edits have to be shifted by the width of the 'winner'
                    int d = 0;          // 0 is used in case of 'mod'
                    if ((from2.add == true) && (from2.rem == false))       // If 'add' ...
                    {
                        d = from2.width + from2.widthOtherSide;     // Justification: either 'width' or 'widthOtherSide' is zero
                    }
                    if ((from2.add == false) && (from2.rem == true))       // If 'rem' ...
                    {
                        d = -(from2.width + from2.widthOtherSide);
                    }
                    for (int j = i1; j < _dlt1.edits.Count; j++)
                    {
                        _dlt1.edits[j].pos_r += d;    // Align _dlt1
                    }
                    i2++;
                }
                else
                {
                    throw new Exception("Wrong 'winner'");
                }
            }

            ApplyEdits();   // Applies this.edits to this.from to calculate this.to

            return true;
        }

        private void _CalculateWidthOtherSide()
        {
            for (int i = 0; i < this.edits.Count; i++)
            {
                if ((this.edits[i].add == true) && (this.edits[i].rem == false))
                {
                    this.edits[i].widthOtherSide = this.edits[i].width_r;
                }
                if ((this.edits[i].add == false) && (this.edits[i].rem == true))
                {
                    this.edits[i].widthOtherSide_r = this.edits[i].width;
                }
            }
        }

        // Only for debug: print a list of string edits List<Util.StrCompareEdit>
        public string PrintEdits(string header, string file, bool append)
        {
            StringBuilder sb = new StringBuilder();
            string s = "\n" + header;
            sb.Append(s);
            Debug.Print(header);

            for (int i = 0; i < this.edits.Count; i++)
            {
                DItem edF = this.edits[i];
                if ((edF.add == true) && (edF.rem == true))
                {
                    s = "MOD";
                }
                else if (edF.add == true)
                {
                    s = "ADD";
                }
                else if (edF.rem == true)
                {
                    s = "REM";
                }
                if (IsStringComparison())
                {
                    s += String.Format(" {0},{1} - {2},{3}    '{4}'-'{5}'", edF.pos, edF.width, edF.pos_r, edF.width_r, edF.data_str, edF.data_str_r);
                }
                else
                {
                    s += String.Format(" {0},{1} - {2},{3}    '{4}'-'{5}'", edF.pos, edF.width, edF.pos_r, edF.width_r, string.Join(",", edF.data_strList), string.Join(",", edF.data_strList_r));
                }
                sb.Append("\n" + s);
                Debug.Print(s);
            }
            if (file != null)
            {
                Util.FileWrite(file, sb.ToString(), append);
            }
            return sb.ToString();
        }

        // Returns a Paragraph with highlighted differences (for string-comparison)
        public Paragraph TextHighlight_str(bool from, string descr, bool keepAligned)
        {
            Paragraph paragraph1 = new Paragraph();
            paragraph1.Inlines.Add(new Run(descr));
            if (this.edits.Count == 0)
            {
                if (from)
                {
                    paragraph1.Inlines.Add(new Run(this.from));
                }
                else
                {
                    paragraph1.Inlines.Add(new Run(this.to));
                }
                return paragraph1;
            }
            int currPos = 0;    // Current position in 'text'
            if (from)
            {
                foreach (DItem ed in this.edits)
                {
                    if (ed.pos > currPos)
                    {
                        paragraph1.Inlines.Add(new Run(this.from.Substring(currPos, ed.pos - currPos)));   // Substring with no edits
                    }
                    currPos = ed.pos;
                    if (ed.rem)
                    {
                        Run r = new Run(this.from.Substring(ed.pos, ed.width));
                        r.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFF0A0");    // Light-orange
                        paragraph1.Inlines.Add(new Bold(r));
                        currPos += ed.width;
                    }
                    else if (keepAligned && ed.add)
                    {
                        Run r = new Run(new string(' ', ed.widthOtherSide));
                        r.Background = Brushes.LightGray;
                        paragraph1.Inlines.Add(r);
                    }
                }
                paragraph1.Inlines.Add(new Run(this.from.Substring(currPos, this.from.Length - currPos)));    // Last part (with no edits)
            }
            else
            {
                foreach (DItem ed in this.edits)
                {
                    if (ed.pos_r > currPos)
                    {
                        paragraph1.Inlines.Add(new Run(this.to.Substring(currPos, ed.pos_r - currPos)));   // Substring with no edits
                    }
                    currPos = ed.pos_r;
                    if (ed.add)
                    {
                        Run r = new Run(this.to.Substring(ed.pos_r, ed.width_r));
                        r.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFF0A0");    // Light-orange
                        paragraph1.Inlines.Add(new Bold(r));
                        currPos += ed.width_r;
                    }
                    else if (keepAligned && ed.rem)
                    {
                        Run r = new Run(new string(' ', ed.widthOtherSide_r));
                        r.Background = Brushes.LightGray;
                        paragraph1.Inlines.Add(r);
                    }
                }
                paragraph1.Inlines.Add(new Run(this.to.Substring(currPos, this.to.Length - currPos)));    // Last part (with no edits)
            }
            return paragraph1;
        }

        // Returns a Paragraph with highlighted differences (for string-list-comparison)
        public Paragraph TextHighlight_strList(bool from, bool keepAligned)
        {
            Paragraph p = new Paragraph();
            if (this.edits.Count == 0)
            {
                if (from)
                {
                    p.Inlines.Add(new Run(string.Join("\r\n", this.fromList)));
                }
                else
                {
                    p.Inlines.Add(new Run(string.Join("\r\n", this.toList)));
                }
                return p;
            }
            int currPos = 0;    // Current position in 'text'
            if (from)
            {
                foreach (DItem ed in this.edits)
                {
                    if (ed.pos > currPos)
                    {
                        p.Inlines.Add(new Run(string.Join("\r\n", this.fromList.GetRange(currPos, ed.pos - currPos)) + "\r\n"));   // Substring with no edits
                    }
                    currPos = ed.pos;
                    if (ed.rem)
                    {
                        Run r = new Run(string.Join("\r\n", this.fromList.GetRange(ed.pos, ed.width)) + "\r\n");
                        r.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFF0A0");    // Light-orange
                        p.Inlines.Add(new Bold(r));
                        currPos += ed.width;
                    }
                    else if (keepAligned && ed.add)
                    {
                        // todo: non va bene: se sono più righe, non ci sono i "\r\n"
                        Run r = new Run((new string('*', ed.widthOtherSide).Replace("*", new string(' ', 80))) + "\r\n");   // todo: probably slow - to be verified
                        r.Background = Brushes.LightGray;
                        p.Inlines.Add(r);
                    }
                }
                p.Inlines.Add(new Run(string.Join("\r\n", this.fromList.GetRange(currPos, this.fromList.Count - currPos))));    // Last part (with no edits)
            }
            else
            {
                foreach (DItem ed in this.edits)
                {
                    if (ed.pos_r > currPos)
                    {
                        p.Inlines.Add(new Run(string.Join("\r\n", this.toList.GetRange(currPos, ed.pos_r - currPos)) + "\r\n"));   // Substring with no edits
                    }
                    currPos = ed.pos_r;
                    if (ed.add)
                    {
                        Run r = new Run(string.Join("\r\n", this.toList.GetRange(ed.pos_r, ed.width_r)) + "\r\n");
                        r.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FFF0A0");    // Light-orange
                        p.Inlines.Add(new Bold(r));
                        currPos += ed.width_r;
                    }
                    else if (keepAligned && ed.rem)
                    {
                        // todo: non va bene: se sono più righe, non ci sono i "\r\n"
                        Run r = new Run((new string('*', ed.widthOtherSide_r).Replace("*", new string(' ', 80))) + "\r\n");   // todo: probably slow - to be verified
                        r.Background = Brushes.LightGray;
                        p.Inlines.Add(r);
                    }
                }
                p.Inlines.Add(new Run(string.Join("\r\n", this.toList.GetRange(currPos, this.toList.Count - currPos))));    // Last part (with no edits)
            }
            return p;
        }
    };

}
