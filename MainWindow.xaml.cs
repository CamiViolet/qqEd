using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace qqEd
{
    /// <summary>
    /// Interaction logic for qqEdit.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region PRIVATE_MEMBERS
        private bool skipEvents = false;
        public Configuration config;

        // Data objects:
        // - textDbFilter
        // - cmbDb (working data: cmbDb_ItemsSource)
        // - cmbSearch
        // - textFindings (working data: dbContent_wt (non filtered), findings_wt (filtered by cmbSearch))

        // Data flow:
        // - textDbFilter => cmbDb (trigger event_LoadCmbDb)
        // - cmbDb => dbContent_wt (non filtered) (trigger event_LoadDbContent_wt)
        // - dbContent_wt + cmbSearch => findings_wt (filtered) (trigger event_LoadFindings_wt)
        // - findings_wt => textFindings (trigger event_LoadFindings)

        // Events to trigger the state machine
        bool event_LoadCmbDb = false;
        bool event_LoadDbContent_wt = false;
        bool event_LoadFindings_wt = false;
        bool event_LoadFindings = false;

        // Working data are prepared and then copyed to the GUI
        List<string> dbContent_wt;
        List<string> findings_wt;

        string param_text = "";

        // Combo boxes source data
        List<string> cmbFilter_ItemsSource = new List<string>();
        //List<string> cmbFileFilter_ItemsSource = new List<string>();
        List<string> cmbDb_ItemsSource = new List<string>();

        string lastClicked;

        // Every actions is called by OnTimerStateMachine(), based on active events (event_*)
        System.Timers.Timer timerStateMachine;

        System.Timers.Timer timerAlignScroll;
        System.Timers.Timer timerTextBoxStatus;

        // RichTextBox myButton = new RichTextBox();
        // RichTextBox myButto2n = new RichTextBox();

        // List<string> text0;
        // List<string> text1;

        List<EditItem> editItems = new List<EditItem>();

        int pivot = 0;      // Index of the pivot editItems[] => editItems[pivot].isPivot = True

        #endregion      // PRIVATE_MEMBERS

        #region CONSTRUCTOR
        public MainWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);    // To close window using 'Esc' (Escape) key
        }
        #endregion      // CONSTRUCTOR

        #region FORM_CALLBACKS

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load from Configuration file
            skipEvents = true;
            config = Configuration.Instance;
            config.Load(this, textFiles);


            // textDbFilter.Text = "";    // This is not persisted
            // settings.UserSettingsRead(this, ref cmbSearch, ref cmbFilter_ItemsSource, cmbSearch.ContextMenu.Items);
            // //settings.UserSettingsRead(this, ref cmbFileFilter, ref cmbFileFilter_ItemsSource, cmbFileFilter.ContextMenu.Items);
            // settings.UserSettingsRead(this, ref cmbDb, ref cmbDb_ItemsSource);      // 'cmbDb' is persisted only to retrieve the current text

            // chkAlignText.IsChecked = settings.ReadBool("chkAlignText");



            timerAlignScroll = new System.Timers.Timer();
            timerAlignScroll.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerAlignScroll);
            timerAlignScroll.Interval = 100;
            timerAlignScroll.Enabled = true;

            timerStateMachine = new System.Timers.Timer();
            timerStateMachine.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerStateMachine);
            timerStateMachine.Interval = 250;
            timerStateMachine.Enabled = true;

            // timerTextBoxStatus = new System.Timers.Timer();
            // timerTextBoxStatus.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerTextBoxStatus);
            // timerTextBoxStatus.Interval = 500;
            // timerTextBoxStatus.Enabled = true;

            if (this.param_text != "")
            {
                // cmbSearch.Text = this.param_text;  // Override 'textFilter' if passed as parameter
            }

            event_LoadCmbDb = true;

            skipEvents = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            config.Save(this, textFiles);

            config.SaveToFile();
        }
        #endregion      // FORM_CALLBACKS

        #region ACCESSORS

        #endregion      // ACCESSORS

        #region PRIVATE_METHODS

        // Add graphincs objects to the GUI
        private void CreateGuiObjects()
        {
            Grid grd2 = new Grid();
            grd2.ShowGridLines = true;
            Grid.SetRow(grd2, 1);
            Grid.SetColumn(grd2, 0);
            grd1.Children.Add(grd2);

            int i = 0;
            foreach (EditItem qqei in editItems)
            {
                ColumnDefinition col0 = new ColumnDefinition();
                grd2.ColumnDefinitions.Add(col0);

                qqei.txtStatus = new TextBox();
                qqei.txtStatus.Height = 60;
                qqei.txtStatus.Margin = new Thickness(10, 10, 10, 10);
                qqei.txtStatus.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(qqei.txtStatus, 0);
                Grid.SetColumn(qqei.txtStatus, i);
                grd2.Children.Add(qqei.txtStatus);

                qqei.textBox = new RichTextBox();
                qqei.textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                qqei.textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                qqei.textBox.Document.PageWidth = 2000;

                ContextMenu cm = new ContextMenu();
                MenuItem mi = new MenuItem();
                mi.Header = "Open";
                cm.Items.Add(mi);
                mi.Click += new System.Windows.RoutedEventHandler(qqei.MEFile_Open_Click);
                qqei.textBox.ContextMenu = cm;


                qqei.textBox.Margin = new Thickness(10, 80, 10, 10);
                qqei.textBox.FontFamily = new FontFamily("Courier New");
                Grid.SetRow(qqei.textBox, 0);
                Grid.SetColumn(qqei.textBox, i);
                grd2.Children.Add(qqei.textBox);

                i++;
            }
        }

        #endregion      # PRIVATE_METHODS

        #region GUI_CALLBACKS

        private void buttonDiff_Click(object sender, RoutedEventArgs e)
        {
            foreach (EditItem qqei in editItems)
            {
                editItems[0].Copy_GUI_to_textGui();
                editItems[1].Copy_GUI_to_textGui();

                editItems[0].Copy_textRtb_to_textMain();
                editItems[1].Copy_textRtb_to_textMain();

                // for debug
                // string s0 = editItems[0].GetContent();
                // List<string> l0 = editItems[0].GetContent2();

                // Compare textMain-s
                // todo: generalize: compare pivot (0) to all others
                StrDelta dlt = new StrDelta(editItems[0].textMain.strList, editItems[1].textMain.strList);
                int distance = dlt.Compare();

                // for debug
                // string s1 = editItems[0].GetContent();
                // List<string> l1 = editItems[0].GetContent2();

                // Calculate 'paragraph' + update screen
                // todo: generalize: calculate for every editItems[]
                Paragraph paragraph = dlt.TextHighlight_strList(true, true);
                // fix this editItems[0].Copy_StrDelta_to_GUI(paragraph);

                paragraph = dlt.TextHighlight_strList(false, true);
                // fix this editItems[1].Copy_StrDelta_to_GUI(paragraph);

                // for debug
                // string s2 = editItems[0].GetContent();
                // List<string> l2 = editItems[0].GetContent2();
            }
        }


        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            // Create qqEditItem objects
            editItems = new List<EditItem>();
            string[] split = textFiles.Text.Split(',');
            foreach (string file in split)
            {
                EditItem ei = new EditItem();
                ei.file = file;
                ei.lineStart = 0;
                ei.lineEnd = int.MaxValue;
                editItems.Add(ei);
            }

            CreateGuiObjects();     // Add EditItem objects to the GUI

            // Load from file
            foreach (EditItem ei in editItems)
            {
                ei.Copy_file_to_textFile();         // Load file into textFile
                ei.Copy_textFile_to_textMain();     // Copy textFile to textMain
                ei.Copy_textFile_to_GUI();          // Copy textFile to GUI
            }
        }


        private void buttonMerge_Click(object sender, RoutedEventArgs e)
        {
            // Handle pivot info
            EditItem ei_pivot = editItems[this.pivot];
            for (int i = 0; i < editItems.Count; i++)
            {
                editItems[i].isPivot = (i == this.pivot);
            }

            // Copy_GUI_to_textGui i.e. copy the content of the RichTextBox to textGui
            foreach (EditItem ei in editItems)
            {
                ei.Copy_GUI_to_textGui();
            }

            // Calculate dlt0 = delta between textMain and textGui i.e. what the user has modified manually
            foreach (EditItem ei in editItems)
            {
                ei.dlt0 = new StrDelta(ei.textMain.strList, ei.textGui.strList);
                ei.dlt0.Compare();
                ei.dlt0.PrintEdits("\ndlt0", @"C:\temp\report.txt", false);
                if (ei.isPivot == false)
                {
                    // todo: user cannot modify non-pivot EditItems => in this case delta must be null
                }
            }

            // Calculate dlt1 = delta between pivot and other editItems[]
            foreach (EditItem ei in editItems)
            {
                if (ei.isPivot)
                {
                    continue;
                }
                ei.dlt1 = new StrDelta(ei_pivot.textMain.strList, ei.textMain.strList);
                ei.dlt1.Compare();
                ei.dlt1.PrintEdits("\ndlt1", @"C:\temp\report.txt", true);
            }

            // Merge
            foreach (EditItem ei in editItems)
            {
                if (ei.isPivot)
                {
                    continue;
                }
                ei.dltM = new StrDelta(ei_pivot.dlt0);      // Initialize dltM = dlt0
                ei.dltM.Merge(ei.dlt1);                     // Merge dlt1 to dtl0
                // todo: handle conflicts
                ei.dltM.PrintEdits("\ndltM", @"C:\temp\report.txt", true);
                
                ei.Copy_dltM_to_GUI();      // Copy merge result to the GUI
            }

            // todo: ask user to approve the merge. If approved => update txtMain. If not approved => revert (i.e. copy txtMain to GUI)
        }

        private void buttonReport_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            // TextPointer firstVisibleChar = MEFiles[0].textBox.GetPositionFromPoint(new Point(0, 0), true);
            // int pos = firstVisibleChar.DocumentStart.GetOffsetToPosition(firstVisibleChar);
            int caretPos = Util.Rtb_GetCaretPos(editItems[0].textBox);
            sb.Append(String.Format("\npos={0}", caretPos));

            // TextPointer caretPos = MEFiles[0].textBox.CaretPosition;
            // TextPointer lineStartPos = caretPos.GetLineStartPosition(0);
            // int caretColumnPos = Math.Max(lineStartPos.GetOffsetToPosition(caretPos) - 1, 0);
            int caretColumnPos = Util.Rtb_GetCaretColumnPos(editItems[0].textBox);
            sb.Append(String.Format("\ncaretColumnPos={0}", caretColumnPos));

            // TextPointer caretLineStart = caretPos.GetLineStartPosition(0);
            // // caretPos.get
            // TextPointer pp = MEFiles[0].textBox.Document.ContentStart.GetLineStartPosition(0);
            // int caretLinePos = 1;
            // while (true)
            // {
            //     if (caretLineStart.CompareTo(pp) < 0)
            //     {
            //         break;
            //     }
            //     int result;
            //     pp = pp.GetLineStartPosition(1, out result);
            //     if (result == 0)
            //     {
            //         break;
            //     }
            //     caretLinePos++;
            // }
            int caretLinePos = Util.Rtb_GetCaretLinePos(editItems[0].textBox);
            sb.Append(String.Format("\ncaretLinePos={0}", caretLinePos));

            // string lineText = new TextRange(caretPos.GetLineStartPosition(0), caretPos.GetLineStartPosition(1)).Text;
            string lineText = Util.Rtb_GetCaretLineText(editItems[0].textBox);
            sb.Append(String.Format("\nlineText=\"{0}\"", lineText.Trim()));

            sb.Append("\n");
            BlockCollection bc = editItems[0].textBox.Document.Blocks;
            List<Block> bs = bc.ToList();
            foreach (Block b in bs)
            {
                sb.Append("\nBlock/Paragraph");
                Paragraph p = b as Paragraph;
                InlineCollection ic = p.Inlines;
                List<Inline> inls = ic.ToList();
                foreach (Inline inl in inls)
                {
                    sb.Append("\n    Inline/Run");
                    Run r = inl as Run;
                    if (r == null)
                    {
                        Bold bld = inl as Bold;
                        if (bld == null)
                        {
                            throw new Exception(String.Format("buttonReport_Click: Bold=null"));
                        }
                        else
                        {
                            if (bld.Inlines.ToList().Count != 1)
                            {
                                throw new Exception(String.Format("buttonReport_Click: bld.Inlines.ToList().Count != 1"));
                            }
                            foreach (Inline inl2 in bld.Inlines.ToList())
                            {
                                sb.Append("\n        Inline/Bold/Run");
                                Run rr = inl2 as Run;
                                if (rr == null)
                                {
                                    sb.Append(String.Format("\n            Run=null"));
                                }
                                else
                                {
                                    sb.Append(String.Format("\n            Run.Text.Length={0} Run.Text={1}", rr.Text.Length, rr.Text.Replace("\r", @"\r").Replace("\n", @"\n"))); // .Replace("\n", @"\n")));
                                }
                            }
                        }
                    }
                    else
                    {
                        sb.Append(String.Format("\n        Run.Text.Length={0} Run.Text=\"{1}\"", r.Text.Length, r.Text.Replace("\r", @"\r").Replace("\n", @"\n"))); // .Replace("\n", @"\n")));
                    }
                }
            }
            string text = editItems[0].GetContent();
            sb.Append(String.Format("\n\nText.Length={0} Text=\"{1}\"", text.Length, text));
            Util.FileWrite(@"C:\temp\report.txt", sb.ToString());
        }

        private void buttonSaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            // foreach (QQEditItem qqei in editItemsList)
            // {
            //     qqei.UpdateChangedVF();
            //     if (qqei.GetChangedVF())
            //     {
            //         Util.FileWrite(qqei.file, qqei.GetContent2());
            //     }
            // }
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            // to close window using 'Esc' key
            if (e.Key == Key.Escape)
                Close();
        }


        #endregion      // GUI_CALLBACKS

        #region TIMERS

        public void OnTimerAlignScroll(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Align text boxes scroll value
            if (editItems.Count()>=2)
            {
                textBoxStatus.Dispatcher.Invoke(() =>
                {
                    editItems[1].textBox.ScrollToVerticalOffset(editItems[0].textBox.VerticalOffset);
                });
            }
        }

        // State machine: call the sequence of update actions
        public void OnTimerStateMachine(Object source, System.Timers.ElapsedEventArgs e)
        {

            if ((event_LoadCmbDb
                || event_LoadDbContent_wt
                || event_LoadFindings
                || event_LoadFindings_wt) == false)
            {
                return;
            }

            timerStateMachine.Enabled = false;          // Disable timer to avoid function reentrance

            foreach (EditItem ei in editItems)
            {
                ei.StateMachine();
            }

            // cmbSearch.Dispatcher.Invoke(() =>
            // {
            //     Mouse.OverrideCursor = Cursors.Wait;
            // });
            // 
            // if (event_LoadCmbDb)
            // {
            //     event_LoadCmbDb = false;
            //     LoadDbList();                   // Get the list of databases, and load cmbDb
            //     event_LoadDbContent_wt = true;
            // }
            // if (event_LoadDbContent_wt)
            // {
            //     event_LoadDbContent_wt = false;
            //     Load_symb0_wt();                            // Load the database in dbContent_wt (search filter not applied)
            //     event_LoadFindings_wt = true;
            // }
            // if (event_LoadFindings_wt)
            // {
            //     event_LoadFindings_wt = false;
            //     Load_Findings_wt();
            //     event_LoadFindings = true;
            // }
            // if (event_LoadFindings)
            // {
            //     event_LoadFindings = false;
            //     Load_txtFindings();
            // }
            // 
            // cmbSearch.Dispatcher.Invoke(() =>
            // {
            //     Mouse.OverrideCursor = null;
            // });
            timerStateMachine.Enabled = true;
        }

        #endregion      // TIMERS
    }
}
