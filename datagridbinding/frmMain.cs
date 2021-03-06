﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RaptorDB.Common;
using SampleViews;

namespace datagridbinding
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }


        IRaptorDB rap;

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.DoubleBuffered(true);
            frmStartup f = new frmStartup();
            if (f.ShowDialog() == DialogResult.OK)
            {
                rap = f._rap;

                Query();
            }
        }

        void TextBox1KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
                Query();
        }

        private void Query()
        {
            int c = textBox1.Text.IndexOf(',');
            string viewname = textBox1.Text.Substring(0, c);
            string args = textBox1.Text.Substring(c + 1);

            try
            {
                DateTime dt = FastDateTime.Now;
                var q = rap.Query(viewname, args.Trim());
                toolStripStatusLabel2.Text = "Query time (sec) = " + FastDateTime.Now.Subtract(dt).TotalSeconds;
                dataGridView1.DataSource = q.Rows;
                toolStripStatusLabel1.Text = "Count = " + q.Count.ToString("#,0");
                stsError.Text = "";
            }
            catch (Exception ex)
            {
                stsError.Text = ex.Message;
                dataGridView1.DataSource = null;
                toolStripStatusLabel1.Text = "Count = 0";
                toolStripStatusLabel2.Text = "Query time (sec) = 0";
            }
        }

        private void sumQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int c = rap.Count("SalesItemRows", "product = \"prod 1\"");

            DateTime dt = FastDateTime.Now;
            var q = //rap.Query(typeof(SalesItemRowsView), (LineItem l) => (l.Product == "prod 1" || l.Product == "prod 3"));
                rap.Query<SalesItemRowsViewRowSchema>(x => x.Product == "prod 1" || x.Product == "prod 3");
            //List<SalesItemRowsView.RowSchema> list = q.Rows.Cast<SalesItemRowsView.RowSchema>().ToList();
            var res = from item in q.Rows//list
                      group item by item.Product into grouped
                      select new
                      {
                          Product = grouped.Key,
                          TotalPrice = grouped.Sum(product => product.Price),
                          TotalQTY = grouped.Sum(product => product.QTY)
                      };

            var reslist = res.ToList();
            dataGridView1.DataSource = reslist;
            toolStripStatusLabel2.Text = "Query time (sec) = " + FastDateTime.Now.Subtract(dt).TotalSeconds;
            toolStripStatusLabel1.Text = "Count = " + q.Count.ToString("#,0");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();// shutdown();
        }

        private void shutdown()
        {
            if (rap != null)
                rap.Shutdown();
            //this.Close();
        }

        private object _lock = new object();
        private void insert100000DocumentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //RaptorDB.Global.SplitStorageFilesMegaBytes = 50;
            lock (_lock)
            {
                DialogResult dr = MessageBox.Show("Do you want to insert?", "Continue?", MessageBoxButtons.OKCancel, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2);
                if (dr == System.Windows.Forms.DialogResult.Cancel)
                    return;
                toolStripProgressBar1.Value = 0;
                DateTime dt = FastDateTime.Now;
                int count = 100000;
                int step = 5000;
                toolStripProgressBar1.Maximum = (count / step) + 1;
                Random r = new Random();
                for (int i = 0; i < count; i++)
                {
                    var inv = CreateInvoice(i);
                    if (i % step == 0)
                        toolStripProgressBar1.Value++;
                    rap.Save(inv.ID, inv);
                }
                MessageBox.Show("Insert done in (sec) : " + FastDateTime.Now.Subtract(dt).TotalSeconds);
                toolStripProgressBar1.Value = 0;
            }
        }

        private static SalesInvoice CreateInvoice(int i)
        {
            var inv = new SalesInvoice()
            {
                Date = Faker.DateTimeFaker.BirthDay(),// FastDateTime.Now.AddMinutes(r.Next(60)),
                Serial = i % 10000,
                CustomerName = Faker.NameFaker.Name(),// "Me " + i % 10,
                NoCase = "Me " + i % 10,
                Status = (byte)(i % 4),
                Address = Faker.LocationFaker.Street(), //"df asd sdf asdf asdf",
                Approved = i % 100 == 0 ? true : false
            };
            inv.Items = new List<LineItem>();
            for (int k = 0; k < 5; k++)
                inv.Items.Add(new LineItem() { Product = "prod " + k, Discount = 0, Price = 10 + k, QTY = 1 + k });
            return inv;
        }

        private void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool b = rap.Backup();
            MessageBox.Show("Backup done");
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rap.Restore();
        }

        public class objclass
        {
            public string val;
        }
        string prod3 = "prod 3";
        private void serverSideSumQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //string prod1 = "prod 1";
            objclass c = new objclass() { val = "prod 3" };
            //decimal i = 20;

            //var q = rap.Count(typeof(SalesItemRowsView), 
            //    (LineItem l) => (l.Product == prod1 || l.Product == prod3) && l.Price.Between(10,i)
            //    );

            DateTime dt = FastDateTime.Now;

            var qq = rap.ServerSide<LineItem>(Views.ServerSide.Sum_Products_based_on_filter_args,
                //"product = \"prod 1\""
                //(LineItem l) => (l.Product == c.val || l.Product == prod3 ) 
                x => x.Product == c.val || x.Product == prod3
                ).ToList();
            dataGridView1.DataSource = qq;
            toolStripStatusLabel2.Text = "Query time (sec) = " + FastDateTime.Now.Subtract(dt).TotalSeconds;
            toolStripStatusLabel1.Text = "Count = " + qq.Count.ToString("#,0");
        }

        private void KVHFtest()
        {
            //var r = (rap as RaptorDB.RaptorDB);
            var kv = rap.GetKVHF();

            int c = kv.CountHF();

            //if (c == 0)
            //{
            //    DateTime dt = DateTime.Now;
            //    for (int i = 0; i < 1000; i++)
            //    {
            //        var o = CreateInvoice(i);
            //        kv.SetObjectHF(i.ToString(), o);// new byte[100000]);
            //    }
            //    MessageBox.Show("time = " + DateTime.Now.Subtract(dt).TotalSeconds);
            //}
            //else
            //{
            //    for(int i = 0; i < 1000; i++)
            //    {
            //        var o = (SalesInvoice) kv.GetObjectHF("" + i);
            //        var id = o.Serial;
            //        if(id != i)
            //        {
            //            MessageBox.Show("not equal");
            //            break;
            //        }
            //    }
            //}
            if(c==0)
            {
                kv.SetObjectHF("00", 100);
                kv.SetObjectHF("01", 101);
            }
            else
            {
                kv.SetObjectHF("00", 102);
            }

            var g = kv.GetObjectHF("00");

            //for (int i = 0; i < 100; i++)
            //kv.DeleteKeyHF(i.ToString());

            //g = kv.GetObjectHF("1009");
            //MessageBox.Show(""+kv.CountHF());

            //foreach (var f in Directory.GetFiles("d:\\pp", "*.*"))
            //{
            //kv.SetObjectHF(f, File.ReadAllBytes(f));
            //}

            //kv.CompactStorageHF();

            //foreach (var f in Directory.GetFiles("d:\\pp", "*.*"))
            //{
            //    var o = kv.GetObjectHF(f);
            //    File.WriteAllBytes(f.Replace("\\pp\\", "\\ppp\\"), o as byte[]);
            //}
            //bool b = kv.ContainsHF("aa");
            //var keys = kv.GetKeysHF();
            //foreach(var o in r.KVHF.EnumerateObjects())
            //{
            //    string s = o.GetType().ToString();
            //}
        }

        class ppp
        {
            public int i = 100;
        }
        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GC.Collect();
            var llll = rap.Query<SalesInvoiceViewRowSchema>(x => x.CustomerName == "ruth" && x.Serial.Between(1600, 1630), 0, 100);

            KVHFtest();
            var ind = rap.Query<SalesInvoiceViewRowSchema>(x =>
                x.Date.Year.In(2000)
                &&
                x.Date.Month.In(4));

            var id = new int[] { 20, 30, 40 };
            var iin = rap.Query<SalesInvoiceViewRowSchema>(x => x.Serial.In(
               // new int[] { 20,30, 40} 
               id
            //20,30,40
            ));
            var p = new ppp[2];
            p[0] = new ppp();
            p[1] = new ppp();
            var pp = new ppp();
            var d1 = DateTime.Parse("2001-1-1");
            var d2 = DateTime.Parse("2010-1-1");
            var ooooooooo = rap.Query<SalesInvoiceViewRowSchema>(x => x.Date.Between("2001-1-1", "2010-1-1") && x.Status == 2);
            ooooooooo = rap.Query<SalesInvoiceViewRowSchema>(x => x.Date.Between(d1, d2) && x.Status == 2);
            ooooooooo = rap.Query<SalesInvoiceViewRowSchema>(x => x.Serial.Between(1, 3));

            int cc = rap.Count<SalesInvoiceViewRowSchema>(x => x.Serial < pp.i);
            var tt = p[1].i;
            cc = rap.Count<SalesInvoiceViewRowSchema>(x => x.Serial < tt);

            var t = rap.Query<SalesInvoiceViewRowSchema>(x => false);
            var ss = rap.FullTextSearch("woodland -oak");

            int c = rap.Count<SalesInvoiceViewRowSchema>(x => x.Serial < 100);
            c = rap.Count<SalesInvoiceViewRowSchema>(x => x.Serial != 100);
            c = rap.Count("SalesInvoice", "serial != 100");
            var q = rap.Query<SalesInvoiceViewRowSchema>(x => x.Serial < 100, 0, 10, "serial desc");
            //var p = rap.Query("SalesInvoice");
            //var pp = rap.Query(typeof(SalesInvoiceView));
            //var ppp = rap.Query(typeof(SalesItemRowsView.RowSchema));
            //var pppp = rap.Query(typeof(SalesInvoiceView), (SalesInvoiceView.RowSchema r) => r.Serial < 10);
            //var ppppp = rap.Query(typeof(SalesInvoiceView.RowSchema), (SalesInvoiceView.RowSchema r) => r.Serial < 10);
            //var pppppp = rap.Query<SalesInvoiceView.RowSchema>("serial <10");
            //Guid g = new Guid("82997e60-f8f4-4b37-ae35-02d033512673");
            var qq = rap.Query<SalesInvoiceViewRowSchema>(x => x.docid == new Guid("82997e60-f8f4-4b37-ae35-02d033512673"));
            dataGridView1.DataSource = q.Rows;

            //int i = rap.ViewDelete<SalesInvoiceViewRowSchema>(x => x.Serial == 0);
            //var qqq= rap.Query<SalesInvoiceViewRowSchema>(x => );
            //SalesInvoiceViewRowSchema s = new SalesInvoiceViewRowSchema();
            //s.docid = Guid.NewGuid();
            //s.CustomerName = "hello";
            //rap.ViewInsert<SalesInvoiceViewRowSchema>(s.docid, s);
            //q= rap.Query<SalesInvoiceView.RowSchema>("serial <100");
            //string s = q.Rows[0].CustomerName;

            //perftest();

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            shutdown();
        }

        private void freememoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rap.FreeMemory();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var id = (Guid)dataGridView1.Rows[e.RowIndex].Cells["docid"].Value;

            var s = fastJSON.JSON.ToNiceJSON( rap.Fetch(id) , new fastJSON.JSONParameters {UseExtensions= false, UseFastGuid = false });

            MessageBox.Show(s, "JSON Value");
        }

        //private void perftest()
        //{
        //    DateTime dt = DateTime.Now;

        //    for (int i = 0; i < 100000; i++)
        //    {
        //        var s = new SalesInvoiceViewRowSchema();
        //        s.docid = Guid.NewGuid();
        //        s.Address = Faker.LocationFaker.Street();
        //        s.CustomerName = Faker.NameFaker.Name();
        //        s.Date = Faker.DateTimeFaker.BirthDay();
        //        s.Serial = i % 1000;
        //        s.Status = (byte)(i % 5);
        //        rap.ViewInsert(s.docid, s);
        //    }
        //    MessageBox.Show("time = " + DateTime.Now.Subtract(dt).TotalSeconds);
        //}
    }
}