using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace qqEd
{
    // This class contains a RichTextBox that displays a portion of a text file.
    public class EditItem
    {
        public string file;
        public int lineStart;
        public int lineEnd;
        public string descr;
        public bool isPivot;        // True if current instance is the pivot (see qqEdit.pivot)
        public Paragraph paragraph;
        public StrDelta dlt0;            // Delta between textMain and textGui
        public StrDelta dlt1;            // Delta between pivot.textMain and this.textMain
        public StrDelta dltM;            // Merge i.e. delta between pivot.textMain and this.textGui
        public TextBox txtStatus;
        public RichTextBox textBox;

        // This class contains a single version of the text.
        // Possible versions are textFile, textCurrent, textRtb.
        // Different versions of the text are needed to implement merging, revert, ...
        public class Instance
        {
            public List<string> strList = new List<string>();
            public string str = null;
            public int hash = -1;
            public int[] hashArray = null;

            public Instance()
            {
            }

            public Instance(Instance ttt)
            {
                this.strList = new List<string>(ttt.strList);
                this.str = ttt.str;
                this.hash = ttt.hash;
                this.hashArray = ttt.hashArray;
            }
        }

        public Instance textFile = new Instance();     // A copy of the file (see Copy_file_to_textFile() and )
        public Instance textMain = new Instance();     // Main version (see Copy_textFile_to_textMain(), Copy_textRtb_to_textMain())
        public Instance textGui = new Instance();      // A copy of 'textBox' (GUI) (see Copy_textRtb_to_GUI() and Copy_GUI_to_textGui())

        // public List<string> text_File = new List<string>();    // 'Version File': original version of the text. Loaded from the file.
        // private string text_File_str;
        // private int text_File_HashCode;
        // private bool changedVF;         // True if current text and 'Version File' are different. The flag is updated on request by UpdateChangedVF()

        // private List<string> textVM;    // 'Version Merge': result of the last merge. To be used as base for next merge.
        // private string textVM_str;
        // private int textVM_HashCode;
        // private bool changedVM;         // True if current text and 'Version Merge' are different. The flag is updated on request by UpdateChangedVM()

        public enum Status
        {
            NotInitialized,
            File,           // 'text' is aligned with the copy on the file (i.e status=Aligned after a load or after a save)
            ModByInt,       // 'text' has been modified using the qqEdit internal editor
            ModByExt,       // 'text' has been modified using an external editor
            Merged          // 'text' has been merged
        };
        public Status status = Status.NotInitialized;

        public EditItem()
        {
        }

        public EditItem(EditItem qqei)
        {
            this.file = qqei.file;
            this.lineStart = qqei.lineStart;
            this.lineEnd = qqei.lineEnd;
            this.descr = qqei.descr;
            this.textFile = new Instance(qqei.textFile);
            // this.text_File = new List<string>(qqei.text_File);
            // this.text_File_str = qqei.text_File_str;
            // this.text_File_HashCode = qqei.text_File_HashCode;
            // this.changedVF = qqei.changedVF;
            this.status = qqei.status;
            // textBox = new RichTextBox();
            // textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            // textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            // textBox.Document.PageWidth = 2000;
            // 
            // ContextMenu cm = new ContextMenu();
            // MenuItem mi = new MenuItem();
            // mi.Header = "Open";
            // cm.Items.Add(mi);
            // mi.Click += new System.Windows.RoutedEventHandler(this.MEFile_Open_Click);
            // textBox.ContextMenu = cm;
        }

        public void MEFile_Open_Click(object sender, RoutedEventArgs e)
        {
            RichTextBox rtp = this.textBox;
            int caretLinePos = Util.Rtb_GetCaretLinePos(rtp);
            Util.FileOpenInTextEditor(this.file, String.Format("{0}", this.lineStart + caretLinePos));
        }

        // Load textFile from the file system
        public void Copy_file_to_textFile()
        {
            Util.FileRead(this.file, this.lineStart, this.lineEnd, out this.textFile.strList);   // EOL characters are discarded. Last line of the file, if empty, is discarded.
            this.textFile.str = string.Join("\r\n", this.textFile.strList);
            this.textFile.hash = this.textFile.str.GetHashCode();
            this.textFile.hashArray = Util.Hash_StringList2DigestArray(this.textFile.strList, false);
            this.status = Status.File;
        }

        // Copy the content of the RichTextBox to textGui
        public void Copy_GUI_to_textGui()
        {
            this.textGui.str = Util.Rtb_GetContent(this.textBox);
            // this.text_Current.strList = this.text_Current.str.Split('\n').ToList();
            this.textGui.strList = this.textGui.str.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            this.textGui.hash = this.textGui.str.GetHashCode();
            this.textGui.hashArray = Util.Hash_StringList2DigestArray(this.textGui.strList, false);
        }

        // Copy textFile to textMain
        public void Copy_textFile_to_textMain()
        {
            this.textMain = new Instance(this.textFile);
        }

        // Copy textFile to textMain
        public void Copy_textFile_to_GUI()
        {
            Util.Rtb_SetContent(this.textBox, this.textFile.str);
        }

        // Copy merge result to the GUI
        public void Copy_dltM_to_GUI()
        {
            Paragraph paragraph = this.dltM.TextHighlight_strList(false, true);
            Util.Rtb_SetContent(this.textBox, paragraph);
        }

        public void Copy_textRtb_to_textMain()
        {
            this.textMain = new Instance(this.textGui);
        }

        public bool GetChangedVF()
        {
            return false; //  this.changedVF;
        }

        public string GetContent()
        {
            return Util.Rtb_GetContent(this.textBox);
        }

        public string GetContentV0()
        {
            return this.textFile.str;
        }

        public List<string> GetContent2()
        {
            string text = Util.Rtb_GetContent(this.textBox);
            return text.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
        }

        // State machine: handle the Status
        public void StateMachine()
        {
            // switch (this.status)
            // {
            //     case Status.NotInitialized:
            //         break;
            // 
            //     case Status.File:
            //         if (Util.Rtb_GetContent(this.textBox) != this.text_File.str)
            //         {
            //             this.status = Status.ModByInt;
            //         }
            //         break;
            // 
            //     case Status.ModByInt:
            //         break;
            // 
            //     case Status.ModByExt:
            //         break;
            // 
            //     case Status.Merged:
            //         break;
            // 
            //     default:
            //         throw new Exception(String.Format("Illegal qqEditItem.Status={0}", this.status));
            // }
            txtStatus.Dispatcher.Invoke(() =>
            {
                // this.UpdateChangedVF();
                txtStatus.Text = String.Format("File: {0}\nLine start/end: {1}/{2}\nStatus: {3}", this.file, this.lineStart, this.lineEnd, this.status);
            });
        }
    }
}
