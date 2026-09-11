using HammerTrackPro2.Models;

namespace HammerTrackPro2.Forms;

public sealed class SaleCorrectionForm : Form
{
    private readonly NumericUpDown _bidder=new(){Minimum=1,Maximum=999999};private readonly NumericUpDown _quantity=new(){Minimum=1,Maximum=1000000,Value=1};private readonly NumericUpDown _price=new(){DecimalPlaces=2,Maximum=100000000};private readonly TextBox _reason=new(){Width=360};private readonly CheckBox _remove=new(){Text="Remove this sale without a replacement buyer",AutoSize=true};
    public SaleCorrectionRequest? Request{get;private set;}
    public SaleCorrectionForm(InvoiceLine line,int currentBidder)
    {
        Text="Correct Customer Sale";Width=560;Height=390;StartPosition=FormStartPosition.CenterParent;_bidder.Value=currentBidder;_quantity.Value=line.Quantity;_price.Value=line.HammerPrice;
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=2,RowCount=7,BackColor=Color.White};table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));table.Controls.Add(new Label{Text=$"Lot #{line.LotNumber} - {line.Description}",AutoSize=true,Font=new Font("Segoe UI",12F,FontStyle.Bold)},0,0);table.SetColumnSpan(table.GetControlFromPosition(0,0)!,2);Add(table,"Correct bidder",_bidder,1);Add(table,"Quantity",_quantity,2);Add(table,"Price each",_price,3);table.Controls.Add(_remove,1,4);Add(table,"Required reason",_reason,5);
        var buttons=new FlowLayoutPanel{AutoSize=true};var save=new Button{Text="Save Correction",Width=150,Height=42};var cancel=new Button{Text="Cancel",Width=110,Height=42};save.Click+=(_,_)=>Save(line);cancel.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};buttons.Controls.Add(save);buttons.Controls.Add(cancel);table.Controls.Add(buttons,1,6);Controls.Add(table);_remove.CheckedChanged+=(_,_)=>{_bidder.Enabled=_quantity.Enabled=_price.Enabled=!_remove.Checked;};
    }
    private void Save(InvoiceLine line){if(string.IsNullOrWhiteSpace(_reason.Text)){MessageBox.Show(this,"Enter the reason for this correction.","Sale Correction",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}Request=new SaleCorrectionRequest{SaleId=line.SaleId,BidderNumber=_remove.Checked?null:Decimal.ToInt32(_bidder.Value),Quantity=Decimal.ToInt32(_quantity.Value),QuantityPerDeal=Decimal.ToInt32(_quantity.Value),Deals=1,HammerPrice=_price.Value,Reason=_reason.Text.Trim()};DialogResult=DialogResult.OK;Close();}
    private static void Add(TableLayoutPanel table,string label,Control control,int row){table.RowStyles.Add(new RowStyle(SizeType.AutoSize));table.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,10,8,8)},0,row);table.Controls.Add(control,1,row);}
}
