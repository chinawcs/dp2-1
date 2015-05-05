﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using DigitalPlatform.Xml;
using DigitalPlatform.CommonControl;
using DigitalPlatform.IO;
using DigitalPlatform;
using System.Diagnostics;

namespace dp2Circulation
{
    /// <summary>
    /// 编辑一行动作的对话框
    /// </summary>
    internal partial class OneActionDialog : Form
    {
        /// <summary>
        /// 当前界面语言代码
        /// </summary>
        public string Lang = "zh";

        /*
<?xml version="1.0" encoding="utf-8" ?>
<root>
  <action element="index" type="">
    <caption lang="zh">编号</caption>
  </action>
         * */
        /// <summary>
        /// 配置动作事项的 XmlDocument
        /// </summary>
        public XmlDocument CfgDom = null;

        /// <summary>
        /// 调用本对话框前， ListView 中已经使用过的字段名。用于限制本对话框列出的字段名，避免发生重复
        /// </summary>
        public List<string> UsedFieldNames = null;

        /// <summary>
        /// 获得值列表
        /// </summary>
        public event GetValueTableEventHandler GetValueTable = null;
        public string RefDbName = "";


        OneActionCfg _actionCfg = null;   // 当前字段名对应的配置参数

        public OneActionDialog()
        {
            InitializeComponent();
        }

        void OnFieldNameChanged()
        {
            if (this.CfgDom == null)
            {
                // 没有配置文件
                this.comboBox_fieldValue.Enabled = true;
                SetMenu(null);
                this.comboBox_fieldValue.Items.Clear();
                return;
            }

            OneActionCfg cfg = null;

            string strError = "";
            // 根据语言相关的 caption 值或者 <> {} 形态的 element 名， 获取 <action> 配置参数
            // return:
            //      -1  出错
            //      0   没有找到定义
            //      1   找到定义
            int nRet = GetActionCfg(
                this.CfgDom,
                this.comboBox_fieldName.Text,
                out cfg,
                out strError);
            if (nRet == -1)
                throw new Exception(strError);

            this._actionCfg = cfg;

            if (this._actionCfg == null)
            {
                // 没有明确定义
                this.comboBox_fieldValue.Enabled = true;
                SetMenu(null);
                this.comboBox_fieldValue.Items.Clear();
                return;
            }

            SetMenu(this._actionCfg);
            this.comboBox_fieldValue.Items.Clear();
        }

        // 根据 type 属性值设置 RFC 1123 等菜单
        void SetMenu(OneActionCfg cfg)
        {
            if (cfg == null)
            {
                this.ToolStripMenuItem_rfc1123Single.Enabled = true;
                return;
            }
            if (cfg.Type == "rfc1123")
            {
                this.ToolStripMenuItem_rfc1123Single.Enabled = true;
            }
            else
            {
                this.ToolStripMenuItem_rfc1123Single.Enabled = false;
            }
        }

        // 校验数据的正确性
        // return:
        //      -1  出错
        //      0   面板数据没有错
        //      1   面板数据有错
        int Verify(out string strError)
        {
            strError = "";

            if (this._actionCfg != null)
            {
                if (this._actionCfg.Type == "rfc1123"
                    && string.IsNullOrEmpty(this.comboBox_fieldValue.Text) == false)
                {
                    try
                    {
                        DateTimeUtil.FromRfc1123DateTimeString(this.comboBox_fieldValue.Text);
                    }
                    catch (Exception ex)
                    {
                        strError = "时间字符串 '" + this.comboBox_fieldValue.Text + "' 格式不正确，应为 RFC1123 格式";
                        return 1;
                    }
                }
            }
            return 0;
        }

        private void OneActionDialog_Load(object sender, EventArgs e)
        {
            if (this.CfgDom != null)
            {
                FillFieldNameList(this.Lang, 
                    this.CfgDom,
                    this.UsedFieldNames,
                    this.comboBox_fieldName);
                if (string.IsNullOrEmpty(this.comboBox_fieldName.Text) == true
                    && this.comboBox_fieldName.Items.Count > 0)
                    this.comboBox_fieldName.Text = (string)this.comboBox_fieldName.Items[0];
            }

            OnFieldNameChanged();
        }

        internal static string Unquote(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            // 去掉头部字符
            if (strText.Length > 0)
                strText = strText.Substring(1);

            // 去掉末尾字符
            if (strText.Length > 0)
                strText = strText.Substring(0, strText.Length - 1);

            return strText;
        }

        static bool IsEqual(
            XmlDocument cfg_dom,
            string strCaption1,
            string strCaption2)
        {
            if (strCaption1 == strCaption2)
                return true;

            string strElementName1 = "";
            if (strCaption1[0] == '{' || strCaption1[0] == '<')
            {
                strElementName1 = Unquote(strCaption1);
            }

            string strElementName2 = "";
            if (strCaption2[0] == '{' || strCaption2[0] == '<')
            {
                strElementName2 = Unquote(strCaption2);
            }

            // 变换为元素名
            string strError = "";
            int nRet = 0;
            if (string.IsNullOrEmpty(strElementName1) == true)
            {
                nRet = GetElementName(
        cfg_dom,
        strCaption1,
        out strElementName1,
        out strError);
                if (nRet == -1)
                    return false;
            }

            if (string.IsNullOrEmpty(strElementName2) == true)
            {
                nRet = GetElementName(
        cfg_dom,
        strCaption2,
        out strElementName2,
        out strError);
                if (nRet == -1)
                    return false;
            }

            if (string.IsNullOrEmpty(strElementName1) == false
    && string.IsNullOrEmpty(strElementName2) == false)
            {
                if (strElementName1 == strElementName2)
                    return true;
                return false;
            }

            return false;
        }

        // 根据语言相关的 caption 值获取 element 属性值
        internal static int GetElementName(
            XmlDocument cfg_dom,
            string strFieldName,
            out string strElementName,
            out string strError)
        {
            strError = "";
            strElementName = "";

            // 从 CfgDom 中查找元素名
            XmlNode node = cfg_dom.DocumentElement.SelectSingleNode("//caption[text()='" + strFieldName + "']");
            if (node == null)
            {
                strError = "字段名 '" + strFieldName + "' 在配置文件中没有定义";
                return -1;
            }

            strElementName = DomUtil.GetAttr(node.ParentNode, "element");

            return 0;
        }

        // 根据语言相关的 caption 值或者 <> {} 形态的 element 名， 获取 <action> 配置参数
        // return:
        //      -1  出错
        //      0   没有找到定义
        //      1   找到定义
        internal static int GetActionCfg(
            XmlDocument cfg_dom,
            string strFieldName,
            out OneActionCfg cfg,
            out string strError)
        {
            strError = "";
            cfg = null;

            if (string.IsNullOrEmpty(strFieldName) == true)
            {
                strError = "strFieldName 参数不应为空";
                return -1;
            }

            XmlNode cfg_node = null;

            if (strFieldName[0] == '{' || strFieldName[0] == '<')
            {
                string strElement = Unquote(strFieldName);
                cfg_node = cfg_dom.DocumentElement.SelectSingleNode("action[@element='"+strElement+"']");
                if (cfg_node == null)
                {
                    strError = "元素名 '" + strFieldName + "' 在配置文件中没有定义";
                    return 0;
                }
            }
            else
            {
                // 从 CfgDom 中查找元素名
                XmlNode node = cfg_dom.DocumentElement.SelectSingleNode("//caption[text()='" + strFieldName + "']");
                if (node == null)
                {
                    strError = "字段名 '" + strFieldName + "' 在配置文件中没有定义";
                    return 0;
                }
                cfg_node = node.ParentNode;
            }

            cfg = new OneActionCfg();
            cfg.Element = DomUtil.GetAttr(cfg_node, "element");
            cfg.Type = DomUtil.GetAttr(cfg_node, "type");
            cfg.List = DomUtil.GetAttr(cfg_node, "list");

            return 1;
        }

        internal static void FillFieldNameList(
            string strLang,
            XmlDocument cfg_dom, 
            List<string> used_fieldnames,
            ComboBox combobox)
        {
            if (cfg_dom == null || cfg_dom.DocumentElement == null)
                return;

            XmlNodeList nodes = cfg_dom.DocumentElement.SelectNodes("action");
            foreach (XmlNode node in nodes)
            {
                string strCaption = DomUtil.GetCaption(strLang, node);

#if NO
                if (used_fieldnames != null
                    && used_fieldnames.IndexOf(strCaption) != -1)
                    continue;
#endif

                // 比较事项是否等同
                if (used_fieldnames != null)
                {
                    bool bFound = false;
                    foreach (string s in used_fieldnames)
                    {
                        bool bRet = IsEqual(cfg_dom,
                            strCaption,
                            s);
                        if (bRet == true)
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (bFound == true)
                        continue;
                }

                combobox.Items.Add(strCaption);
            }
        }

        public string FieldName
        {
            get
            {
                return this.comboBox_fieldName.Text;
            }
            set
            {
                this.comboBox_fieldName.Text = value;

                OnFieldNameChanged();   // 是否有必要?
            }
        }

        public string FieldValue
        {
            get
            {
                return this.comboBox_fieldValue.Text;
            }
            set
            {
                this.comboBox_fieldValue.Text = value;
            }
        }

#if NO
        private void ToolStripMenuItem_rfc1123Single_Click(object sender, EventArgs e)
        {
        }

        private void comboBox_fieldName_TextChanged(object sender, EventArgs e)
        {

        }
#endif

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (string.IsNullOrEmpty(this.comboBox_fieldName.Text) == true)
            {
                strError = "尚未指定字段名";
                goto ERROR1;
            }

            // 校验数据的正确性
            // return:
            //      -1  出错
            //      0   面板数据没有错
            //      1   面板数据有错
            int nRet = Verify(out strError);
            if (nRet == -1 || nRet == 1)
                goto ERROR1;

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void ToolStripMenuItem_rfc1123Single_Click_1(object sender, EventArgs e)
        {
            GetTimeDialog dlg = new GetTimeDialog();
            MainForm.SetControlFont(dlg, this.Font, false);

            dlg.RangeMode = false;
            try
            {
                dlg.Rfc1123String = this.comboBox_fieldValue.Text;
            }
            catch
            {
                this.comboBox_fieldValue.Text = "";
            }

            dlg.StartPosition = FormStartPosition.CenterScreen;

            //this.MainForm.AppInfo.LinkFormState(dlg, "ChangeOrderActionDialog_gettimedialog");
            dlg.ShowDialog(this);
            //this.MainForm.AppInfo.UnlinkFormState(dlg);

            if (dlg.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                return;

            this.comboBox_fieldValue.Text = dlg.Rfc1123String;

        }

        private void comboBox_fieldName_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnFieldNameChanged();
        }

        private void comboBox_fieldName_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_fieldName.Invalidate();
        }

        private void comboBox_fieldValue_SizeChanged(object sender, EventArgs e)
        {
            this.comboBox_fieldValue.Invalidate();
        }

        private void comboBox_fieldValue_DropDown(object sender, EventArgs e)
        {
            ComboBox combobox = (ComboBox)sender;
            if (combobox.Items.Count == 0 
                && this._actionCfg != null
                && string.IsNullOrEmpty(this._actionCfg.List) == false)
                FillDropDown(combobox);
        }

        int m_nInDropDown = 0;
        void FillDropDown(ComboBox combobox)
        {
            if (this._actionCfg == null
                || string.IsNullOrEmpty(this._actionCfg.List) == true)
                return;

            // 防止重入
            if (this.m_nInDropDown > 0)
                return;

            Cursor oldCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            this.m_nInDropDown++;
            try
            {
                if (combobox.Items.Count <= 0
                    && this.GetValueTable != null)
                {
                    GetValueTableEventArgs e1 = new GetValueTableEventArgs();
                    e1.DbName = this.RefDbName;
                    e1.TableName = this._actionCfg.List;

                    this.GetValueTable(this, e1);

                    // combobox.Items.Add("<不改变>");

                    if (e1.values != null)
                    {
                        for (int i = 0; i < e1.values.Length; i++)
                        {
                            combobox.Items.Add(e1.values[i]);
                        }
                    }
                    else
                    {
                        // combobox.Items.Add("{not found}");
                    }
                }
            }
            finally
            {
                this.Cursor = oldCursor;
                this.m_nInDropDown--;
            }
        }

        private void comboBox_fieldValue_TextChanged(object sender, EventArgs e)
        {
            if (this._actionCfg != null
                && string.IsNullOrEmpty(this._actionCfg.List) == false)
                Global.FilterValueList(this, (Control)sender);
        }

    }
}