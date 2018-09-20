using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DropDownloader
{
    public partial class pastaspref : Form
    {
        bool initial,working;
        List<string> paths;
        public pastaspref(string json)
        {
            InitializeComponent();
            initial = true;
            working = false;
            paths = new List<string>();
            treeView1.CheckBoxes = true;
            treeView1.AfterCheck += TreeView1_AfterCheck; ;
            try
            {
                DisplayTreeView(JToken.Parse(json), @"Pastas :\");
            }
            catch
            {

            }
            try
            {
                treeView1.Nodes[0].Checked = true;
                initial = false;
                if (f1.prefpastas == null)
                {
                    f1.prefpastas = new List<Pastapref>();
                    foreach (string s in paths)
                    {
                        f1.prefpastas.Add(new Pastapref(s, true));

                    }
                }
                else
                {
                    working = true;
                    Console.WriteLine("no add");
                    f1.prefpastas.Print();
                    foreach (string s in paths)
                    {
                        Pastapref.addifNew(f1.prefpastas, s);
                    }

                    Console.WriteLine("AfterAdd");
                    f1.prefpastas.Print();
                    Console.WriteLine("-------");
                    List<TreeNode> nodes = treeView1.GetAllNodes();
                    Console.WriteLine("kkkk");
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        string a = nodes[i].FullPath.Replace(@"Pastas :\", "");
                        Pastapref pf = Pastapref.getboolvalue(f1.prefpastas, a);
                        if (pf != null && pf.check != nodes[i].Checked)
                        {
                            nodes[i].Checked = pf.check;
                        }
                    }
                    working = false;
                }
            }
            catch(Exception xp)
            {
                MessageBox.Show("Erro ao gerar pastas "+xp.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TreeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if(!working)
                for (int i = 0; i < e.Node.Nodes.Count; i++)
                    e.Node.Nodes[i].Checked = e.Node.Checked;
               
           
            if (initial)
            {
                if (e.Node.Nodes.Count == 0)
                {
                    string path = e.Node.FullPath;
                    Console.WriteLine("caminho node " + path);
                    paths.Add(path.Replace(@"Pastas :\", ""));
                }

           }else
            {
                Pastapref.checkifexist(f1.prefpastas, e.Node.FullPath.Replace(@"Pastas :\", ""),e.Node.Checked);
                if (File.Exists(f1.pastapath)) File.Delete(f1.pastapath);
                StreamWriter sw = new StreamWriter(f1.pastapath);
                sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(f1.prefpastas));
                sw.Close();

            }
            //if (f1.prefpastas!=null)
           // f1.prefpastas.Print();

        }

       

        private void DisplayTreeView(JToken root, string rootName)
        {
            treeView1.BeginUpdate();
            try
            {
                treeView1.Nodes.Clear();
                var tNode = treeView1.Nodes[treeView1.Nodes.Add(new TreeNode(rootName))];
                tNode.Tag = root;

                AddNode(root, tNode);

                treeView1.ExpandAll();
            }
            finally
            {
                treeView1.EndUpdate();
            }
        }
        private void AddNode(JToken token, TreeNode inTreeNode)
        {
            if (token == null)
                return;
            if (token is JValue)
            {
                if (!string.IsNullOrEmpty( token.ToString()))
                {
                    TreeNode tr = new TreeNode(token.ToString());
                    tr.Checked = true;
                    var childNode = inTreeNode.Nodes[inTreeNode.Nodes.Add(tr)];
                    childNode.Tag = token;
                }
            }
            else if (token is JObject)
            {
                var obj = (JObject)token;
                foreach (var property in obj.Properties())
                {
                    var childNode = inTreeNode.Nodes[inTreeNode.Nodes.Add(new TreeNode(property.Name) { Checked = true })];
                    childNode.Tag = property;
                    AddNode(property.Value, childNode);
                }
            }
            else if (token is JArray)
            {
                var array = (JArray)token;
                for (int i = 0; i < array.Count; i++)
                {
                    var childNode = inTreeNode;
                    childNode.Tag = array[i];
                    AddNode(array[i], childNode);
                }
            }
            else
            {
                Debug.WriteLine(string.Format("{0} not implemented", token.Type)); // JConstructor, JRaw
            }
        }
        


    }



public static class extensions
    {
    public static List<TreeNode> GetAllNodes(this TreeView _self)
{
    List<TreeNode> result = new List<TreeNode>();
    foreach (TreeNode child in _self.Nodes)
    {
        result.AddRange(child.GetAllNodes());
    }
    return result;
}

public static List<TreeNode> GetAllNodes(this TreeNode _self)
{
    List<TreeNode> result = new List<TreeNode>();
    result.Add(_self);
    foreach (TreeNode child in _self.Nodes)
    {
        result.AddRange(child.GetAllNodes());
    }
    return result;
}

        public static void Print(this List<Pastapref> self)
        {
            foreach(Pastapref pf in self)
            {
                Console.WriteLine(pf.ToString());
            }
        }
}
}


