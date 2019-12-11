﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;
using System.Xml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;
using System.Threading;
using RevisoftApplication;
using System.Data;

namespace UserControls
{
  public partial class ucExcel_CassaContanteAltreValute : UserControl
  {
    public int id;
  

    private DataTable dati = null;

   
    private string _ID = "";

    private bool _ReadOnly = false;

    GenericTable gtCassaContante = null;

    public ucExcel_CassaContanteAltreValute()
    {
      CultureInfo culture = CultureInfo.CreateSpecificCulture("it-IT");
      InitializeComponent();
    }

    public bool ReadOnly
    {
      set
      {
        _ReadOnly = value;
      }
    }
   
    public void LoadDataSource(string ID, string IDCliente, string IDSessione)
    {

        id = int.Parse(ID.ToString());
        cBusinessObjects.idcliente = int.Parse(IDCliente.ToString());
        cBusinessObjects.idsessione = int.Parse(IDSessione.ToString());
    
        _ID = ID ;
     

      ArrayList Al = new ArrayList();

      dati = cBusinessObjects.GetData(id, typeof(Excel_CassaContanteAltreValute));
      foreach (DataRow dtrow in dati.Rows)
      {
        if (!Al.Contains(dtrow["CreditoEsistente"].ToString()))
        {
            Al.Add(dtrow["CreditoEsistente"].ToString());
        }
      }

      if (Al.Count == 0)
      {
        Al.Add("Divise");
      }

      foreach (string item in Al)
      {
        TabItem ti = new TabItem();
        ti.Header = item;

        tabControl.Items.Add(ti);
      }

      TabItem tiout = new TabItem();
      tiout.Header = App.NewTabHeaderText;

      tabControl.Items.Add(tiout);

      gtCassaContante = new GenericTable(tblCassaContante, _ReadOnly);

      gtCassaContante.ColumnsAlias = new string[] { "N° Pezzi", "Unitario", "Importo" };
      gtCassaContante.ColumnsValues = new string[] { "numeropezzi", "unitario", "valuta" };
      gtCassaContante.ColumnsWidth = new double[] { 1.0, 1.0, 1.0 };
      gtCassaContante.ColumnsMinWidth = new double[] { 0.0, 0.0, 0.0 };
      gtCassaContante.ColumnsTypes = new string[] { "string", "money", "money" };
      gtCassaContante.ColumnsAlignment = new string[] { "right", "right", "right" };
      gtCassaContante.ConditionalReadonly = new bool[] { false, false, true };
      gtCassaContante.ConditionalAttribute = "";
      gtCassaContante.ColumnsHasTotal = new bool[] { false, false, true };
      gtCassaContante.AliasTotale = "Totale";
      gtCassaContante.ColumnAliasTotale = 1;
      gtCassaContante.xml = false;
      gtCassaContante.dati = dati;
      gtCassaContante.TotalToBeCalculated += GtCassaContante_TotalToBeCalculated;

    }

    public int Save()
    {
            return cBusinessObjects.SaveData(id, dati, typeof(Excel_CassaContanteAltreValute));
     }

    private void AggiungiNodo(string Alias,  string Codice, string Header)
    {
        dati.Rows.Add(id,cBusinessObjects.idcliente, cBusinessObjects.idsessione, Header);
       
    }

    private void GtCassaContante_TotalToBeCalculated(object sendername, EventArgs e)
    {
      if (((string)sendername).Split('_').Count() < 2)
      {
        return;
      }

      string idcolumn = ((string)sendername).Split('_')[1];
      string idrow = ((string)sendername).Split('_')[2];

      if (idcolumn == "0" || idcolumn == "1")
      {
        double unitario = 0.0;
        double numeropezzi = 0.0;
        double euro = 0.0;

        double.TryParse(gtCassaContante.GetValue("1", idrow), out unitario);
        double.TryParse(gtCassaContante.GetValue("0", idrow), out numeropezzi);

        euro = unitario * numeropezzi;

        gtCassaContante.SetValue("2", idrow, cBusinessObjects.ConvertNumber(euro.ToString()));
      }

      GenerateTotal();
    }

    private void GenerateTotal()
    {
      txtTotaleComplessivo.Text = gtCassaContante.GenerateSpecificTotal("2");

      double totale = 0.0;
      double.TryParse(txtTotaleComplessivo.Text, out totale);

      double tassocambio = 0.0;
      double.TryParse(txtTassoDiCambio.Text, out tassocambio);

      txtControvaloreInEuro.Text = cBusinessObjects.ConvertNumber((totale * tassocambio).ToString());

      double saldo = 0.0;
      double.TryParse(txtSaldoSchedaContabile.Text, out saldo);

      txtDifferenza.Text = cBusinessObjects.ConvertNumber(((totale * tassocambio) - saldo).ToString());
      foreach (DataRow dtrow in dati.Rows)
      {
        if (dtrow["CreditoEsistente"].ToString()== ((TabItem)(tabControl.SelectedItem)).Header.ToString())
        {
                    if (!String.IsNullOrEmpty(txtTotaleComplessivo.Text))
                        dtrow["txtTotaleComplessivo"] = txtTotaleComplessivo.Text;
                    if (!String.IsNullOrEmpty(txtDifferenza.Text))
                        dtrow["txtDifferenza"] = txtDifferenza.Text;
                    if (!String.IsNullOrEmpty(txtSaldoSchedaContabile.Text))
                        dtrow["txtSaldoSchedaContabile"] = txtSaldoSchedaContabile.Text;
                    if (!String.IsNullOrEmpty(txtTassoDiCambio.Text))
                        dtrow["txtTassoDiCambio"] = txtTassoDiCambio.Text;
                    if (!String.IsNullOrEmpty(txtControvaloreInEuro.Text))
                        dtrow["txtControvaloreInEuro"] = txtControvaloreInEuro.Text;
         }

      }

    }

    private void AddRowErroriRilevati(object sender, RoutedEventArgs e)
    {
      AggiungiNodo("", "", ((TabItem)(tabControl.SelectedItem)).Header.ToString());
      gtCassaContante.filtercolumn = "CreditoEsistente";
      gtCassaContante.filtervalue = ((TabItem)(tabControl.SelectedItem)).Header.ToString();
      gtCassaContante.GenerateTable();
    }

    private void DeleteRowErroriRilevati(object sender, RoutedEventArgs e)
    {
      gtCassaContante.DeleteRow();
      return;
    }

 

    private void obj_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (_ReadOnly)
      {
        MessageBox.Show(App.MessaggioSolaScrittura, "Attenzione");
        return;
      }
    }

    private void obj_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (_ReadOnly)
      {
        MessageBox.Show(App.MessaggioSolaScrittura, "Attenzione");
        return;
      }
    }

    private void txtPeriodoDiRiferimento_LostFocus(object sender, RoutedEventArgs e)
    {
      if (tabControl.SelectedItem == null)
      {
        return;
      }
      foreach (DataRow dtrow in dati.Rows)
        {
            if (dtrow["CreditoEsistente"].ToString() ==((TabItem)(tabControl.SelectedItem)).Header.ToString())
            {
                dtrow["CreditoEsistente"] = txtCreditoEsistente.Text;
            }
        }
    }

    private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count > 0 && (e.AddedItems[0]).GetType().Name == "TabItem")
      {
        if (((string)(((TabItem)(e.AddedItems[0])).Header)) == App.NewTabHeaderText)
        {
          if (_ReadOnly)
          {
            MessageBox.Show(App.MessaggioSolaScrittura, "Attenzione");
            return;
          }

          var dialog = new wInputBox("Inserire Nome della nuova Tabella");
          dialog.ShowDialog();
          if (!dialog.diagres)
          {
            return;
          }
          string newHeader = dialog.ResponseText;

          if (newHeader == "")
          {
            MessageBox.Show("Attenzione, Nome non valido");
            tabControl.SelectedIndex = 0;
            return;
          }

          foreach (TabItem item in tabControl.Items)
          {
            if (((string)(item.Header)) == newHeader)
            {
              MessageBox.Show("Attenzione, Nome già esistente");
              tabControl.SelectedIndex = 0;
              return;
            }
          }

          TabItem ti = new TabItem();
          ti.Header = newHeader;

     
          tabControl.Items.Insert(tabControl.Items.Count - 1, ti);
          tabControl.SelectedIndex = tabControl.Items.Count - 2;

        }
        else
        {

            foreach (DataRow dtrow in dati.Rows)
            {
                if (dtrow["CreditoEsistente"].ToString() == ((TabItem)(e.AddedItems[0])).Header.ToString())
                {
                            txtCreditoEsistente.Text = dtrow["CreditoEsistente"].ToString();
                            txtTotaleComplessivo.Text=dtrow["txtTotaleComplessivo"].ToString();
                            txtDifferenza.Text=dtrow["txtDifferenza"].ToString();
                            txtSaldoSchedaContabile.Text=dtrow["txtSaldoSchedaContabile"].ToString();
                            txtTassoDiCambio.Text=dtrow["txtTassoDiCambio"].ToString();
                            txtControvaloreInEuro.Text=dtrow["txtControvaloreInEuro"].ToString();
                }

            }
            gtCassaContante.filtercolumn = "CreditoEsistente";
            gtCassaContante.filtervalue = ((TabItem)(e.AddedItems[0])).Header.ToString();
            gtCassaContante.GenerateTable();
        }
      }
    }

    private void btnDeleteTable_Click(object sender, RoutedEventArgs e)
    {
      if (tabControl.Items.Count <= 2)
      {
        MessageBox.Show("L'unica tabella presente non è cancellabile; usa il comando Cancella Contenuto.", "Attenzione");
        return;
      }

      if (_ReadOnly)
      {
        MessageBox.Show(App.MessaggioSolaScrittura, "Attenzione");
        return;
      }

      if (MessageBox.Show("La tabella verrà cancellata. Procedere?", "Attenzione", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
      {
        string newHeader = ((tabControl.SelectedItem == null) ? "" : ((TabItem)(tabControl.SelectedItem)).Header.ToString());
      
        //string newHeader = txtCreditoEsistente.Text;

        for (int i = dati.Rows.Count - 1; i >= 0; i--)
        {
            DataRow dtrow = dati.Rows[i];
            if (dtrow["CreditoEsistente"].ToString() == newHeader)
                dtrow.Delete();
        }
        this.dati.AcceptChanges();
        

        TabItem oldSelected = ((TabItem)(tabControl.SelectedItem));

        if (tabControl.Items.Count > 0)
        {
          tabControl.SelectedIndex = 0;
        }
        else
        {
          tabControl.SelectedIndex = -1;
        }

        tabControl.Items.Remove(oldSelected);
      }
    }

    private void btnRenameTable_Click(object sender, RoutedEventArgs e)
    {
      if (_ReadOnly)
      {
        MessageBox.Show(App.MessaggioSolaScrittura, "Attenzione");
        return;
      }

      if (tabControl.SelectedItem == null)
      {
        return;
      }

      var dialog = new wInputBox("Inserire Titolo della nuova Tabella");
      dialog.ResponseText = ((TabItem)(tabControl.SelectedItem)).Header.ToString();
      dialog.ShowDialog();

      string newHeader = dialog.ResponseText;

      if (newHeader == "")
      {
        MessageBox.Show("Attenzione, Titolo non valido");
        tabControl.SelectedIndex = 0;
        return;
      }

      foreach (TabItem item in tabControl.Items)
      {
        if (((string)(item.Header)) == newHeader)
        {
          MessageBox.Show("Attenzione, Titolo già esistente");
          tabControl.SelectedIndex = 0;
          return;
        }
      }

     ChangeNameTab(newHeader, ((TabItem)(tabControl.SelectedItem)).Header.ToString());
     gtCassaContante.filtercolumn = "CreditoEsistente";
     gtCassaContante.filtervalue = newHeader;
     gtCassaContante.GenerateTable();

      ((TabItem)(tabControl.SelectedItem)).Header = newHeader;

    }

    private void ChangeNameTab(string newname, string oldheader)
    {
        foreach (DataRow dtrow in dati.Rows)
        {
            if (dtrow["CreditoEsistente"].ToString() == oldheader)
            {
                dtrow["CreditoEsistente"] = newname;   
            }
        }
    }

    private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
      var tabItem = e.Source as TabItem;

      if (tabItem == null)
        return;

      if (tabItem.Header.ToString() == App.NewTabHeaderText)
      {
        return;
      }

      if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
      {
        DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
      }
    }

    private void TabItem_Drop(object sender, DragEventArgs e)
    {
      if (_ReadOnly)
      {
        return;
      }

      var tabItemTarget = e.Source as TabItem;

      if (tabItemTarget.Header.ToString() == App.NewTabHeaderText)
      {
        return;
      }

      var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;

      if (!tabItemTarget.Equals(tabItemSource))
      {
        string sourceHeader = tabItemSource.Header.ToString();
        string targetHeader = tabItemTarget.Header.ToString();

        ChangeNameTab(sourceHeader, sourceHeader);
        ChangeNameTab(targetHeader, targetHeader);

        tabItemTarget.Header = sourceHeader;
        tabItemSource.Header = targetHeader;
        DataTable datiC = dati.Clone();
        foreach (TabItem tb in tabControl.Items)
        {
            foreach (DataRow dtrow in dati.Rows)
            {
                if (dtrow["CreditoEsistente"] != null)
                {
                    if (dtrow["CreditoEsistente"].ToString() == tb.Header.ToString())
                    {
                        DataRow firstNewRow = datiC.NewRow();
                        firstNewRow.ItemArray = dtrow.ItemArray;
                                datiC.Rows.Add(firstNewRow);
                    }
                }
            }
        }
        dati = datiC;
        gtCassaContante.filtercolumn = "CreditoEsistente";
        gtCassaContante.filtervalue = ((TabItem)(tabControl.SelectedItem)).Header.ToString();
        gtCassaContante.GenerateTable();

      }
    }

    private void tabControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      ;
    }

    private void txtSaldoSchedaContabile_LostFocus(object sender, RoutedEventArgs e)
    {
      if (tabControl.SelectedItem == null)
      {
        return;
      }

      txtSaldoSchedaContabile.Text = cBusinessObjects.ConvertNumber(txtSaldoSchedaContabile.Text);

      foreach (DataRow dtrow in dati.Rows)
        {
            if (dtrow["CreditoEsistente"].ToString() == ((TabItem)(tabControl.SelectedItem)).Header.ToString())
            {

                dtrow["txtSaldoSchedaContabile"] = txtSaldoSchedaContabile.Text;
            }

        }
      GenerateTotal();
    }

    private void txtTassoDiCambio_LostFocus(object sender, RoutedEventArgs e)
    {
      if (tabControl.SelectedItem == null)
      {
        return;
      }

      txtTassoDiCambio.Text = cBusinessObjects.ConvertNumber(txtTassoDiCambio.Text.Replace(".", ","));
      foreach (DataRow dtrow in dati.Rows)
        {
            if (dtrow["CreditoEsistente"].ToString() == ((TabItem)(tabControl.SelectedItem)).Header.ToString())
            {
             
                dtrow["txtTassoDiCambio"] = txtTassoDiCambio.Text;
            }

        }
        GenerateTotal();
    }
  }
}
