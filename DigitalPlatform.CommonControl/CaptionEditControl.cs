using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;


using DigitalPlatform.Xml;

namespace DigitalPlatform.CommonControl
{
    /// <summary>
    /// �༭һ�� caption Ԫ�صĽ���ؼ�
    /// </summary>
    public partial class CaptionEditControl : UserControl
    {
        internal const int BASE_ROW_COUNT = 2;

        internal bool m_bFocused = false;

        bool m_bHideSelection = true;


        // ��ʼ�����Դ����
        internal string[] language_codes = new string[] {
            "zh-CN",
            "en",
        };

        public event EventHandler ElementCountChanged = null;

        public event EventHandler SelectedIndexChanged = null;

        public CaptionElement LastClickElement = null;   // ���һ��clickѡ�����Element����

        int m_nInSuspend = 0;

        public List<CaptionElement> Elements = new List<CaptionElement>();

        bool m_bChanged = false;

        public CaptionEditControl()
        {
            InitializeComponent();
        }

        // �Ƿ��б�����?
        internal bool m_bHasTitleLine = true;

        [DefaultValue(true)]
        public bool HasTitleLine
        {
            get
            {
                return this.m_bHasTitleLine;
            }
            set
            {
                this.m_bHasTitleLine = value;

                this.label_topleft.Visible = this.m_bHasTitleLine;
                this.label_language.Visible = this.m_bHasTitleLine;
                this.label_text.Visible = this.m_bHasTitleLine;
            }
        }

        // ���ؽ��:
        //     ������������Զ���������Ϊ true������Ϊ false��Ĭ��ֵΪ false��
        [DefaultValue(false)]
        public override bool AutoScroll
        {
            get
            {
                return base.AutoScroll;
            }
            set
            {
                base.AutoScroll = value;
                this.tableLayoutPanel_main.AutoScroll = value;
            }
        }

        public TableLayoutPanelCellBorderStyle CellBorderStyle
        {
            get
            {
                return this.tableLayoutPanel_main.CellBorderStyle;
            }
            set
            {
                this.tableLayoutPanel_main.CellBorderStyle = value;
            }
        }

        /// <summary>
        /// �����Ƿ������޸�
        /// </summary>
        public bool Changed
        {
            get
            {
                return this.m_bChanged;
            }
            set
            {

                if (this.m_bChanged != value)
                {
                    this.m_bChanged = value;

                    if (value == false)
                        ResetLineColor();
                }
            }
        }

        // ����������б�
        public void FillLanguageList(ComboBox list,
            string[] languages)
        {
            list.Items.Clear();
            if (languages == null
                || languages.Length == 0)
                return;

            foreach (string value in languages)
            {
                list.Items.Add(value);
            }
        }

        void ResetLineColor()
        {
            for (int i = 0; i < this.Elements.Count; i++)
            {
                CaptionElement element = this.Elements[i];
                element.State = ElementState.Normal;
            }
        }

        void RefreshLineColor()
        {
            for (int i = 0; i < this.Elements.Count; i++)
            {
                CaptionElement item = this.Elements[i];
                item.SetLineColor();
            }
        }

        [Category("Appearance")]
        [DescriptionAttribute("HideSelection")]
        [DefaultValue(true)]
        public bool HideSelection
        {
            get
            {
                return this.m_bHideSelection;
            }
            set
            {
                if (this.m_bHideSelection != value)
                {
                    this.m_bHideSelection = value;
                    this.RefreshLineColor(); // ��ʹ��ɫ�ı�
                }
            }
        }


        [Category("Data")]
        [DescriptionAttribute("Xml")]
        [DefaultValue("")]
        public string Xml
        {
            get
            {
                string strXml = "";
                string strError = "";
                int nRet = this.GetXml(out strXml,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
                return strXml;
            }
            set
            {
                string strError = "";
                int nRet = this.SetXml(value,
                    out strError);
                if (nRet == -1)
                    throw new Exception(strError);
            }
        }

        // ��鵱ǰ������ʽ���Ƿ�Ϸ�
        // return:
        //      -1  �����̱�������
        //      0   ��ʽ�д���
        //      1   ��ʽû�д���
        public int Verify(out string strError)
        {
            strError = "";

            if (this.Elements.Count == 0)
            {
                strError = "��һ�����ݶ�û��";
                return 0;
            }


            List<string> langs = new List<string>();

            for (int i = 0; i < this.Elements.Count; i++)
            {
                CaptionElement element = this.Elements[i];
                string strLanguage = element.Language;
                string strValue = element.Value;

                if (String.IsNullOrEmpty(strLanguage) == true
                    && String.IsNullOrEmpty(strValue) == true)
                {
                    strError = "�� " + (i+1).ToString() + " ��Ϊ���У�������ã�Ӧ����ɾ��";
                    return 0;
                }

                if (String.IsNullOrEmpty(strLanguage) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " �е����Դ�����δָ��";
                    return 0;
                }

                if (String.IsNullOrEmpty(strValue) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " �е�����ֵ��δָ��";
                    return 0;
                }

                int index = langs.IndexOf(strLanguage);
                if (index != -1)
                {
                    strError = "�� " + (i + 1).ToString() + " �е����Դ��� '" + strLanguage + "' �� �� " + (index+1).ToString() + " �е��ظ���";
                    return 0;
                }

                langs.Add(strLanguage);
            }

            return 1;
        }

        int GetXml(out string strXml,
            out string strError)
        {
            strError = "";
            strXml = "";

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            for (int i = 0; i < this.Elements.Count; i++)
            {
                CaptionElement element = this.Elements[i];
                string strLanguage = element.Language;
                string strValue = element.Value;

                if (String.IsNullOrEmpty(strLanguage) == true
                    && String.IsNullOrEmpty(strValue) == true)
                    continue;

                if (String.IsNullOrEmpty(strLanguage) == true)
                {
                    strError = "�� " + (i+1).ToString() + " �е����Դ�����δָ��";
                    return -1;
                }

                if (String.IsNullOrEmpty(strValue) == true)
                {
                    strError = "�� " + (i + 1).ToString() + " �е�����ֵ��δָ��";
                    return -1;
                }

                XmlNode node = dom.CreateElement("caption");
                dom.DocumentElement.AppendChild(node);

                DomUtil.SetAttr(node, "lang", strLanguage);
                node.InnerText = strValue;
            }

            strXml = dom.DocumentElement.InnerXml;

            return 0;
        }

        int SetXml(string strXml,
            out string strError)
        {
            strError = "";

            // clear linesԭ������
            this.Clear();
            this.LastClickElement = null;

            if (String.IsNullOrEmpty(strXml) == true)
                return 0;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strXml;
            }
            catch (Exception ex)
            {
                strError = "fragment XMLװ��XmlDocumentFragmentʱ����: " + ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);

            XmlNodeList nodes = dom.DocumentElement.SelectNodes("caption");

            this.DisableUpdate();

            try
            {

                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    CaptionElement element = this.AppendNewElement();

                    element.Language = DomUtil.GetAttr(node, "lang");
                    element.Value = node.InnerText;
                }


                this.Changed = false;
            }
            finally
            {
                this.EnableUpdate();
            }

            return 0;
        }

        public CaptionElement AppendNewElement()
        {
            this.tableLayoutPanel_main.RowCount += 1;
            this.tableLayoutPanel_main.RowStyles.Add(new System.Windows.Forms.RowStyle());

            CaptionElement line = new CaptionElement(this);

            line.AddToTable(this.tableLayoutPanel_main, this.Elements.Count + 1);

            this.Elements.Add(line);
            if (this.ElementCountChanged != null)
                this.ElementCountChanged(this, new EventArgs());

            return line;
        }

        public CaptionElement InsertNewElement(int index)
        {
            this.tableLayoutPanel_main.RowCount += 1;
            this.tableLayoutPanel_main.RowStyles.Insert(index + 1, new System.Windows.Forms.RowStyle());

            CaptionElement line = new CaptionElement(this);

            line.InsertToTable(this.tableLayoutPanel_main, index);

            this.Elements.Insert(index, line);
            if (this.ElementCountChanged != null)
                this.ElementCountChanged(this, new EventArgs());

            line.State = ElementState.New;

            SelectElement(line, true);  // 2008/6/10

            return line;
        }

        public void RemoveElement(int index)
        {
            CaptionElement line = this.Elements[index];

            line.RemoveFromTable(this.tableLayoutPanel_main, index);

            this.Elements.Remove(line);
            if (this.ElementCountChanged != null)
                this.ElementCountChanged(this, new EventArgs());

            if (this.LastClickElement == line)
                this.LastClickElement = null;

            this.Changed = true;
        }

        public void RemoveElement(CaptionElement line)
        {
            int index = this.Elements.IndexOf(line);

            if (index == -1)
                return;

            line.RemoveFromTable(this.tableLayoutPanel_main, index);

            this.Elements.Remove(line);
            if (this.ElementCountChanged != null)
                this.ElementCountChanged(this, new EventArgs());

            if (this.LastClickElement == line)
                this.LastClickElement = null;

            this.Changed = true;
        }

        public void DisableUpdate()
        {
            if (this.m_nInSuspend == 0)
            {
                this.tableLayoutPanel_main.SuspendLayout();
            }

            this.m_nInSuspend++;
        }

        // parameters:
        //      bOldVisible ���Ϊtrue, ��ʾ���Ҫ����
        public void EnableUpdate()
        {
            this.m_nInSuspend--;


            if (this.m_nInSuspend == 0)
            {
                this.tableLayoutPanel_main.ResumeLayout(false);
                this.tableLayoutPanel_main.PerformLayout();
            }
        }

        public void Clear()
        {
            this.DisableUpdate();

            try
            {

                for (int i = 0; i < this.Elements.Count; i++)
                {
                    CaptionElement element = this.Elements[i];
                    ClearOneElementControls(this.tableLayoutPanel_main,
                        element);
                }

                this.Elements.Clear();
                this.tableLayoutPanel_main.RowCount = BASE_ROW_COUNT;    // Ϊʲô��1��
                for (; ; )
                {
                    if (this.tableLayoutPanel_main.RowStyles.Count <= BASE_ROW_COUNT)
                        break;
                    this.tableLayoutPanel_main.RowStyles.RemoveAt(BASE_ROW_COUNT);
                }

            }
            finally
            {
                this.EnableUpdate();
            }

            if (this.ElementCountChanged != null)
                this.ElementCountChanged(this, new EventArgs());

        }

        // ���һ��CaptionElement�����Ӧ��Control
        public void ClearOneElementControls(
            TableLayoutPanel table,
            CaptionElement line)
        {
            // color
            Label label = line.label_color;
            table.Controls.Remove(label);

            // language
            ComboBox language = line.comboBox_language;
            table.Controls.Remove(language);

            // text
            TextBox text = line.textBox_value;
            table.Controls.Remove(text);
        }

        public void SelectAll()
        {
            bool bSelectedChanged = false;

            for (int i = 0; i < this.Elements.Count; i++)
            {
                CaptionElement cur_element = this.Elements[i];
                if ((cur_element.State & ElementState.Selected) == 0)
                {
                    cur_element.State |= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        public List<CaptionElement> SelectedElements
        {
            get
            {
                List<CaptionElement> results = new List<CaptionElement>();

                for (int i = 0; i < this.Elements.Count; i++)
                {
                    CaptionElement cur_element = this.Elements[i];
                    if ((cur_element.State & ElementState.Selected) != 0)
                        results.Add(cur_element);
                }

                return results;
            }
        }

        public List<int> SelectedIndices
        {
            get
            {
                List<int> results = new List<int>();

                for (int i = 0; i < this.Elements.Count; i++)
                {
                    CaptionElement cur_element = this.Elements[i];
                    if ((cur_element.State & ElementState.Selected) != 0)
                        results.Add(i);
                }

                return results;
            }
        }

        public void SelectElement(CaptionElement element,
            bool bClearOld)
        {
            bool bSelectedChanged = false;

            if (bClearOld == true)
            {
                for (int i = 0; i < this.Elements.Count; i++)
                {
                    CaptionElement cur_element = this.Elements[i];

                    if (cur_element == element)
                        continue;   // ��ʱ��������ǰ��

                    if ((cur_element.State & ElementState.Selected) != 0)
                    {
                        cur_element.State -= ElementState.Selected;
                        bSelectedChanged = true;
                    }
                }
            }

            // ѡ�е�ǰ��
            if ((element.State & ElementState.Selected) == 0)
            {
                element.State |= ElementState.Selected;
                bSelectedChanged = true;
            }

            this.LastClickElement = element;

            if (bClearOld == true)
            {
                // ����focus�ǲ����Ѿ�����һ���ϣ�
                // ������ڣ���Ҫ�л�����
                if (element.IsSubControlFocused() == false)
                    element.comboBox_language.Focus();
            }

            if (bSelectedChanged == true)
                OnSelectedIndexChanged();
        }

        public void ToggleSelectElement(CaptionElement element)
        {
            // ѡ�е�ǰ��
            if ((element.State & ElementState.Selected) == 0)
                element.State |= ElementState.Selected;
            else
                element.State -= ElementState.Selected;

            this.LastClickElement = element;

            this.OnSelectedIndexChanged();
        }

        void OnSelectedIndexChanged()
        {
            if (this.SelectedIndexChanged != null)
            {
                this.SelectedIndexChanged(this, new EventArgs());
            }
        }

        public void RangeSelectElement(CaptionElement element)
        {
            bool bSelectedChanged = false;

            CaptionElement start = this.LastClickElement;

            int nStart = this.Elements.IndexOf(start);
            if (nStart == -1)
                return;

            int nEnd = this.Elements.IndexOf(element);

            if (nStart > nEnd)
            {
                // ����
                int nTemp = nStart;
                nStart = nEnd;
                nEnd = nTemp;
            }

            for (int i = nStart; i <= nEnd; i++)
            {
                CaptionElement cur_element = this.Elements[i];

                if ((cur_element.State & ElementState.Selected) == 0)
                {
                    cur_element.State |= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            // �������λ��
            for (int i = 0; i < nStart; i++)
            {
                CaptionElement cur_element = this.Elements[i];

                if ((cur_element.State & ElementState.Selected) != 0)
                {
                    cur_element.State -= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            for (int i = nEnd + 1; i < this.Elements.Count; i++)
            {
                CaptionElement cur_element = this.Elements[i];

                if ((cur_element.State & ElementState.Selected) != 0)
                {
                    cur_element.State -= ElementState.Selected;
                    bSelectedChanged = true;
                }
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        public void DeleteSelectedElements()
        {
            bool bSelectedChanged = false;

            List<CaptionElement> selected_lines = this.SelectedElements;

            if (selected_lines.Count == 0)
            {
                MessageBox.Show(this, "��δѡ��Ҫɾ������");
                return;
            }
            string strText = "";

            if (selected_lines.Count == 1)
                strText = "ȷʵҪɾ���� '" + selected_lines[0].Language + "'? ";
            else
                strText = "ȷʵҪɾ����ѡ���� " + selected_lines.Count.ToString() + " ����?";

            DialogResult result = MessageBox.Show(this,
                strText,
                "CaptionEditControl",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }


            this.DisableUpdate();
            try
            {
                for (int i = 0; i < selected_lines.Count; i++)
                {
                    this.RemoveElement(selected_lines[i]);
                    bSelectedChanged = true;
                }
            }
            finally
            {
                this.EnableUpdate();
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();
        }

        // �����ѡ��Ĳ���Ԫ�ص�XML
        public int GetFragmentXml(
            List<CaptionElement> selected_lines,
            out string strXml,
            out string strError)
        {
            strXml = "";
            strError = "";

            if (selected_lines.Count == 0)
                return 0;

            XmlDocument dom = new XmlDocument();

            dom.LoadXml("<root />");

            for (int i = 0; i < selected_lines.Count; i++)
            {
                CaptionElement line = selected_lines[i];

                string strLanguage = line.Language;

                if (String.IsNullOrEmpty(strLanguage) == true)
                {
                    if (String.IsNullOrEmpty(line.Value) == true)
                        continue;
                    else
                    {
                        strError = "��ʽ��������Ϊ '" + line.Value + "' ����û��ָ�����Դ���";
                        return -1;
                    }
                }

                XmlNode element = dom.CreateElement("caption");
                dom.DocumentElement.AppendChild(element);

                DomUtil.SetAttr(element, "lang", strLanguage);
                element.InnerText = line.Value;

            }

            strXml = dom.DocumentElement.InnerXml;
            return 0;
        }

        // ��Ƭ��XML�а�����Ԫ�أ��滻ָ����������
        // ���selected_lines.Count == 0�����ʾ��nInsertPos��ʼ����
        public int ReplaceElements(
            int nInsertPos,
            List<CaptionElement> selected_lines,
            string strFragmentXml,
            out string strError)
        {
            strError = "";
            bool bSelectedChanged = false;

            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            XmlDocumentFragment fragment = dom.CreateDocumentFragment();
            try
            {
                fragment.InnerXml = strFragmentXml;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            dom.DocumentElement.AppendChild(fragment);


            XmlNode root = dom.DocumentElement;

            int index = 0;  // selected_lines�±� ѡ��lines�����еĵڼ���

            int nTailPos = nInsertPos;   // �����������һ��line�������������е�λ��

            this.DisableUpdate();

            try
            {

                // ���������¼�Ԫ��
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    XmlNode node = root.ChildNodes[i];
                    if (node.NodeType != XmlNodeType.Element)
                        continue;

                    // ���Բ��Ƕ������ֿռ��Ԫ��
                    if (node.Name != "caption")
                        continue;

                    CaptionElement line = null;
                    if (selected_lines != null && index < selected_lines.Count)
                    {
                        line = selected_lines[index];
                        index++;
                    }
                    else
                    {
                        // �����λ�ú������
                        line = this.InsertNewElement(nTailPos);
                    }

                    // ѡ���޸Ĺ���line
                    line.State |= ElementState.Selected;
                    bSelectedChanged = true;

                    nTailPos = this.Elements.IndexOf(line) + 1;

                    line.Language = DomUtil.GetAttr(node, "lang");

                    line.Value = node.InnerText;
                }

                // Ȼ���selected_lines�ж����lineɾ��
                if (selected_lines != null)
                {
                    for (int i = index; i < selected_lines.Count; i++)
                    {
                        this.RemoveElement(selected_lines[i]);
                    }
                }
            }
            finally
            {
                this.EnableUpdate();
            }

            if (bSelectedChanged == true)
                this.OnSelectedIndexChanged();

            return 0;
        }

        private void CaptionEditControl_Enter(object sender, EventArgs e)
        {
            this.m_bFocused = true;
            this.RefreshLineColor();

        }

        private void CaptionEditControl_Leave(object sender, EventArgs e)
        {
            this.m_bFocused = false;
            this.RefreshLineColor();
        }

        // ���Ͻǵ�һ��label��������popupmenu
        private void label_topleft_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            bool bHasClipboardObject = false;
            IDataObject ido = Clipboard.GetDataObject();
            if (ido.GetDataPresent(DataFormats.Text) == true)
                bHasClipboardObject = true;

            //
            menuItem = new MenuItem("�������(&A)");
            menuItem.Click += new System.EventHandler(this.menu_appendElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("����������(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copyRecord_Click);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ���滻������(&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteRecord_Click);
            if (bHasClipboardObject == true)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.label_topleft, new Point(e.X, e.Y));

        }

        // �����׷��һ������
        void menu_appendElement_Click(object sender, EventArgs e)
        {
            NewElement();
        }

        // �����׷��һ������
        public void NewElement()
        {
            CaptionElement element = this.InsertNewElement(this.Elements.Count);

            // ����ɼ���Χ��
            element.ScrollIntoView();

            // ѡ����
            element.Select(1);
        }

        // ����������¼
        void menu_copyRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = "";
            int nRet = this.GetXml(out strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }

            /*
            string strOutXml = "";
            nRet = DomUtil.GetIndentXml(strXml,
                out strOutXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }*/

            Clipboard.SetDataObject(strXml);
        }

        // ճ���滻������¼
        void menu_pasteRecord_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = ClipboardUtil.GetClipboardText();
            int nRet = this.SetXml(strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this, strError);
                return;
            }
        }
    }


    public class CaptionElement
    {
        public CaptionEditControl Container = null;

        // ��ɫ��popupmenu
        public Label label_color = null;

        // ����
        public ComboBox comboBox_language = null;

        // ����
        public TextBox textBox_value = null;

        ElementState m_state = ElementState.Normal;

        public ElementState State
        {
            get
            {
                return this.m_state;
            }
            set
            {
                if (this.m_state != value)
                {
                    this.m_state = value;
                    SetLineColor();
                }
            }
        }

        internal void SetLineColor()
        {
            if ((this.m_state & ElementState.Selected) != 0)
            {
                // û�н��㣬����Ҫ����selection����
                if (this.Container.HideSelection == true
                    && this.Container.m_bFocused == false)
                {
                    // ��������ߣ���ʾ������ɫ
                }
                else
                {
                    this.label_color.BackColor = SystemColors.Highlight;
                    return;
                }
            }


            if ((this.m_state & ElementState.New) != 0)
            {
                this.label_color.BackColor = Color.Yellow;
                return;
            }
            if ((this.m_state & ElementState.Changed) != 0)
            {
                this.label_color.BackColor = Color.LightGreen;
                return;
            }

            this.label_color.BackColor = SystemColors.Window;
        }

        public CaptionElement(CaptionEditControl container)
        {
            this.Container = container;

            label_color = new Label();
            label_color.Dock = DockStyle.Fill;
            label_color.Size = new Size(6, 28);

            // language
            comboBox_language = new ComboBox();
            comboBox_language.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox_language.FlatStyle = FlatStyle.Flat;
            comboBox_language.Dock = DockStyle.Fill;
            comboBox_language.MaximumSize = new Size(150, 28);
            comboBox_language.Size = new Size(100, 28);
            comboBox_language.MinimumSize = new Size(50, 28);
            comboBox_language.DropDownHeight = 300;
            comboBox_language.DropDownWidth = 150;

            comboBox_language.ForeColor = this.Container.tableLayoutPanel_main.ForeColor;

            comboBox_language.Text = "";



            // value
            textBox_value = new TextBox();
            textBox_value.BorderStyle = BorderStyle.None;
            textBox_value.Dock = DockStyle.Fill;
            textBox_value.MinimumSize = new Size(100, 26);  // 26���ܱ��⸲�Ǳ�����
            textBox_value.Margin = new Padding(6, 3, 6, 1);

            textBox_value.ForeColor = this.Container.tableLayoutPanel_main.ForeColor;
        }

        public string Language
        {
            get
            {
                return this.comboBox_language.Text;
            }
            set
            {
                this.comboBox_language.Text = value;
            }
        }

        public string Value
        {
            get
            {
                return this.textBox_value.Text;
            }
            set
            {
                this.textBox_value.Text = value;
            }
        }

        public void AddToTable(TableLayoutPanel table,
            int nRow)
        {
            table.Controls.Add(this.label_color, 0, nRow);
            table.Controls.Add(this.comboBox_language, 1, nRow);
            table.Controls.Add(this.textBox_value, 2, nRow);

            AddEvents();
        }

        void AddEvents()
        {
            // events

            // label_color
            this.label_color.MouseUp -= new MouseEventHandler(label_color_MouseUp);
            this.label_color.MouseUp += new MouseEventHandler(label_color_MouseUp);

            this.label_color.MouseClick -= new MouseEventHandler(label_color_MouseClick);
            this.label_color.MouseClick += new MouseEventHandler(label_color_MouseClick);


            // language
            this.comboBox_language.DropDown -= new EventHandler(comboBox_language_DropDown);
            this.comboBox_language.DropDown += new EventHandler(comboBox_language_DropDown);

            this.comboBox_language.TextChanged -= new EventHandler(comboBox_language_TextChanged);
            this.comboBox_language.TextChanged += new EventHandler(comboBox_language_TextChanged);

            this.comboBox_language.Enter -= new EventHandler(comboBox_language_Enter);
            this.comboBox_language.Enter += new EventHandler(comboBox_language_Enter);

            // value
            this.textBox_value.KeyUp -= new KeyEventHandler(textBox_value_KeyUp);
            this.textBox_value.KeyUp += new KeyEventHandler(textBox_value_KeyUp);

            this.textBox_value.TextChanged -= new EventHandler(textBox_value_TextChanged);
            this.textBox_value.TextChanged += new EventHandler(textBox_value_TextChanged);

            this.textBox_value.Enter -= new EventHandler(textBox_value_Enter);
            this.textBox_value.Enter += new EventHandler(textBox_value_Enter);
        }

        void label_color_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = null;

            int nSelectedCount = this.Container.SelectedIndices.Count;
            bool bHasClipboardObject = false;
            IDataObject ido = Clipboard.GetDataObject();
            if (ido.GetDataPresent(DataFormats.Text) == true)
                bHasClipboardObject = true;

            //
            menuItem = new MenuItem("ǰ��(&I)");
            menuItem.Click += new System.EventHandler(this.menu_insertElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("���(&A)");
            menuItem.Click += new System.EventHandler(this.menu_appendElement_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            //
            menuItem = new MenuItem("ɾ��(&D)");
            menuItem.Click += new System.EventHandler(this.menu_deleteElements_Click);
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("����(&T)");
            menuItem.Click += new System.EventHandler(this.menu_cut_Click);
            if (nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("����(&C)");
            menuItem.Click += new System.EventHandler(this.menu_copy_Click);
            if (nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ������[ǰ](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteInsert_Click);
            if (bHasClipboardObject == true
                && nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("ճ������[��](&P)");
            menuItem.Click += new System.EventHandler(this.menu_pasteInsertAfter_Click);
            if (bHasClipboardObject == true
                && nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);


            menuItem = new MenuItem("ճ���滻(&R)");
            menuItem.Click += new System.EventHandler(this.menu_pasteReplace_Click);
            if (bHasClipboardObject == true
                && nSelectedCount > 0)
                menuItem.Enabled = true;
            else
                menuItem.Enabled = false;
            contextMenu.MenuItems.Add(menuItem);

            // ---
            menuItem = new MenuItem("-");
            contextMenu.MenuItems.Add(menuItem);



            menuItem = new MenuItem("ȫѡ(&A)");
            menuItem.Click += new System.EventHandler(this.menu_selectAll_Click);
            contextMenu.MenuItems.Add(menuItem);


            contextMenu.Show(this.label_color, new Point(e.X, e.Y));
        }

        void menu_insertElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.Elements.IndexOf(this);

            if (nPos == -1)
                throw new Exception("not found myself");

            this.Container.InsertNewElement(nPos);
        }

        void menu_appendElement_Click(object sender, EventArgs e)
        {
            int nPos = this.Container.Elements.IndexOf(this);
            if (nPos == -1)
            {
                throw new Exception("not found myself");
            }

            this.Container.InsertNewElement(nPos + 1);
        }

        // ȫѡ
        void menu_selectAll_Click(object sender, EventArgs e)
        {
            this.Container.SelectAll();
        }

        // ɾ����ǰԪ��
        void menu_deleteElements_Click(object sender, EventArgs e)
        {
            this.Container.DeleteSelectedElements();
        }

        // ����
        void menu_cut_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = "";

            List<CaptionElement> selected_lines = this.Container.SelectedElements;


            // �����ѡ��Ĳ���Ԫ�ص�XML
            int nRet = this.Container.GetFragmentXml(
                selected_lines,
                out strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            Clipboard.SetDataObject(strXml);


            this.Container.DisableUpdate();
            try
            {
                for (int i = 0; i < selected_lines.Count; i++)
                {
                    this.Container.RemoveElement(selected_lines[i]);
                }
            }
            finally
            {
                this.Container.EnableUpdate();
            }
        }

        // ����
        void menu_copy_Click(object sender, EventArgs e)
        {
            string strError = "";
            string strXml = "";
            // �����ѡ��Ĳ���Ԫ�ص�XML
            int nRet = this.Container.GetFragmentXml(
                this.Container.SelectedElements,
                out strXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            Clipboard.SetDataObject(strXml);
        }

        // ճ������[ǰ]
        void menu_pasteInsert_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<CaptionElement> selected_lines = this.Container.SelectedElements;

            int nInsertPos = 0;

            if (selected_lines.Count == 0)
                nInsertPos = 0;
            else
                nInsertPos = this.Container.SelectedIndices[0];

            string strFragmentXml = ClipboardUtil.GetClipboardText();

            int nRet = this.Container.ReplaceElements(
                nInsertPos,
                null,
                strFragmentXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            // ��ԭ��ѡ�е�Ԫ�ر�Ϊû��ѡ��״̬
            for (int i = 0; i < selected_lines.Count; i++)
            {
                CaptionElement line = selected_lines[i];
                if ((line.State & ElementState.Selected) != 0)
                    line.State -= ElementState.Selected;
            }

        }

        // ճ������[��]
        void menu_pasteInsertAfter_Click(object sender, EventArgs e)
        {
            string strError = "";

            List<CaptionElement> selected_lines = this.Container.SelectedElements;

            int nInsertPos = 0;

            if (selected_lines.Count == 0)
                nInsertPos = this.Container.Elements.Count;
            else
                nInsertPos = this.Container.SelectedIndices[0] + 1;

            string strFragmentXml = ClipboardUtil.GetClipboardText();

            int nRet = this.Container.ReplaceElements(
                nInsertPos,
                null,
                strFragmentXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }

            // ��ԭ��ѡ�е�Ԫ�ر�Ϊû��ѡ��״̬
            for (int i = 0; i < selected_lines.Count; i++)
            {
                CaptionElement line = selected_lines[i];
                if ((line.State & ElementState.Selected) != 0)
                    line.State -= ElementState.Selected;
            }

        }


        // ճ���滻
        void menu_pasteReplace_Click(object sender, EventArgs e)
        {
            string strError = "";

            string strFragmentXml = ClipboardUtil.GetClipboardText();

            int nRet = this.Container.ReplaceElements(
                0,
                this.Container.SelectedElements,
                strFragmentXml,
                out strError);
            if (nRet == -1)
            {
                MessageBox.Show(this.Container, strError);
                return;
            }
        }



        // ����ɫlabel�ϵ������
        void label_color_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // MessageBox.Show(this.Container, "left click");
                if (Control.ModifierKeys == Keys.Control)
                {
                    this.Container.ToggleSelectElement(this);
                }
                else if (Control.ModifierKeys == Keys.Shift)
                    this.Container.RangeSelectElement(this);
                else
                {
                    this.Container.SelectElement(this, true);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // �����ǰ�ж���ѡ���򲻱���ʲôl
                // �����ǰΪ����һ��ѡ�����0��ѡ����ѡ��ǰ����
                // ��������Ŀ���Ƿ������
                if (this.Container.SelectedIndices.Count < 2)
                {
                    this.Container.SelectElement(this, true);
                }
            }
        }

        void comboBox_language_DropDown(object sender, EventArgs e)
        {
            if (this.comboBox_language.Items.Count == 0)
                this.FillLanguageList();
        }

        void comboBox_language_TextChanged(object sender, EventArgs e)
        {
            if ((this.State & ElementState.New) == 0)
                this.State |= ElementState.Changed;

            this.Container.Changed = true;
        }

        void textBox_value_Enter(object sender, EventArgs e)
        {
            this.Container.SelectElement(this, true);
        }

        void comboBox_language_Enter(object sender, EventArgs e)
        {
            this.Container.SelectElement(this, true);
        }

        void textBox_value_KeyUp(object sender, KeyEventArgs e)
        {
        }

        void textBox_value_TextChanged(object sender, EventArgs e)
        {
            this.Container.Changed = true;

            if ((this.State & ElementState.New) == 0)
                this.State |= ElementState.Changed;
        }

        public void FillLanguageList()
        {
            this.Container.FillLanguageList(this.comboBox_language,
                this.Container.language_codes);
        }

        // ��Ԫ���������Ŀؼ�ӵ���˽�����ô?
        public bool IsSubControlFocused()
        {
            if (this.comboBox_language.Focused == true)
                return true;

            if (this.textBox_value.Focused == true)
                return true;

            return false;
        }

        // ���뱾Line��ĳ�С�����ǰ��table.RowCount�Ѿ�����
        // parameters:
        //      nRow    ��0��ʼ����
        public void InsertToTable(TableLayoutPanel table,
            int nRow)
        {
            this.Container.DisableUpdate();

            try
            {

                Debug.Assert(table.RowCount ==
                    this.Container.Elements.Count + (CaptionEditControl.BASE_ROW_COUNT + 1), "");

                // ���ƶ��󷽵�
                for (int i = (table.RowCount - 1) - (CaptionEditControl.BASE_ROW_COUNT + 1); i >= nRow; i--)
                {
                    CaptionElement line = this.Container.Elements[i];

                    // color
                    Label label = line.label_color;
                    table.Controls.Remove(label);
                    table.Controls.Add(label, 0, i + 1 + 1);

                    // language
                    ComboBox language = line.comboBox_language;
                    table.Controls.Remove(language);
                    table.Controls.Add(language, 1, i + 1 + 1);

                    // text
                    TextBox text = line.textBox_value;
                    table.Controls.Remove(text);
                    table.Controls.Add(text, 2, i + 1 + 1);

                }

                table.Controls.Add(this.label_color, 0, nRow + 1);
                table.Controls.Add(this.comboBox_language, 1, nRow + 1);
                table.Controls.Add(this.textBox_value, 2, nRow + 1);

            }
            finally
            {
                this.Container.EnableUpdate();
            }

            // events
            AddEvents();
        }

        // �Ƴ���Element
        // parameters:
        //      nRow    ��0��ʼ����
        public void RemoveFromTable(TableLayoutPanel table,
            int nRow)
        {
            this.Container.DisableUpdate();

            try
            {

                // �Ƴ�������صĿؼ�
                table.Controls.Remove(this.label_color);
                table.Controls.Remove(this.comboBox_language);
                table.Controls.Remove(this.textBox_value);

                Debug.Assert(this.Container.Elements.Count
                    == table.RowCount - CaptionEditControl.BASE_ROW_COUNT, "");

                // Ȼ��ѹ���󷽵�
                for (int i = (table.RowCount - CaptionEditControl.BASE_ROW_COUNT) - 1; i >= nRow + 1; i--)
                {
                    CaptionElement line = this.Container.Elements[i];

                    // color
                    Label label = line.label_color;
                    table.Controls.Remove(label);
                    table.Controls.Add(label, 0, i - 1 + 1);


                    // language
                    ComboBox language = line.comboBox_language;
                    table.Controls.Remove(language);
                    table.Controls.Add(language, 1, i - 1 + 1);

                    // text
                    TextBox text = line.textBox_value;
                    table.Controls.Remove(text);
                    table.Controls.Add(text, 2, i - 1 + 1);

                }

                table.RowCount--;
                table.RowStyles.RemoveAt(nRow);

            }
            finally
            {
                this.Container.EnableUpdate();
            }
        }

        // ����ɼ���Χ
        public void ScrollIntoView()
        {
            this.Container.tableLayoutPanel_main.ScrollControlIntoView(this.comboBox_language);
        }

        // ��ѡ��Ԫ��
        // parameters:
        //      nCol    1 ���Դ�����; 2: ������
        public void Select(int nCol)
        {

            if (nCol == 1)
            {
                this.comboBox_language.SelectAll();
                this.comboBox_language.Focus();
                return;
            }

            if (nCol == 2)
            {
                this.textBox_value.SelectAll();
                this.textBox_value.Focus();
                return;
            }

            this.Container.SelectElement(this, true);
        }
    }

}