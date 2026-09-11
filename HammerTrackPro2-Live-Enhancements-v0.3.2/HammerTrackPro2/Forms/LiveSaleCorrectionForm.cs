using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class LiveSaleCorrectionForm : Form
{
    private readonly LiveSaleCorrectionViewModel _viewModel;
    private readonly NumericUpDown _bidder=new(){Minimum=1,Maximum=999999};
    private readonly NumericUpDown _quantity=new(){Minimum=1,Maximum=1000000};
    private readonly NumericUpDown _deals=new(){Minimum=1,Maximum=1000000};
    private readonly NumericUpDown _price=new(){DecimalPlaces=2,Maximum=100000000,ThousandsSeparator=true};
    private readonly TextBox _reason=new(){Dock=DockStyle.Fill};
    public SaleCorrectionRequest? Request{get;private set;}

    public LiveSaleCorrectionForm(LiveSaleHistoryItem sale)
    {
        _viewModel=new LiveSaleCorrectionViewModel(sale);
        Text="Correct Recorded Sale";Width=620;Height=_viewModel.IsWholesale?500:440;StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",11F);
        _bidder.Value=_viewModel.BidderNumber;_quantity.Value=_viewModel.QuantityPerDeal;_deals.Value=_viewModel.Deals;_price.Value=_viewModel.HammerPrice;
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=2,RowCount=_viewModel.IsWholesale?8:7,BackColor=Color.White};
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,180));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        var heading=new Label{Text=_viewModel.Heading,AutoSize=true,Font=new Font("Segoe UI",13F,FontStyle.Bold),ForeColor=Color.FromArgb(24,50,74)};table.Controls.Add(heading,0,0);table.SetColumnSpan(heading,2);
        var buyer=new Label{Text=_viewModel.Buyer,AutoSize=true,ForeColor=Color.DimGray};table.Controls.Add(buyer,0,1);table.SetColumnSpan(buyer,2);
        Add(table,"Correct bidder",_bidder,2);Add(table,_viewModel.IsWholesale?"Quantity per deal":"Quantity",_quantity,3);
        var priceRow=4;
        if(_viewModel.IsWholesale){Add(table,"Deals",_deals,4);priceRow=5;}
        Add(table,"Price each",_price,priceRow);Add(table,"Required reason",_reason,priceRow+1);
        var buttons=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.LeftToRight};var save=new Button{Text="Save Correction",Width=170,Height=44,BackColor=Color.FromArgb(24,96,150),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};var cancel=new Button{Text="Cancel",Width=120,Height=44};save.Click+=(_,_)=>Save();cancel.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};buttons.Controls.Add(save);buttons.Controls.Add(cancel);table.Controls.Add(buttons,1,priceRow+2);Controls.Add(table);AcceptButton=save;CancelButton=cancel;
    }

    private void Save()
    {
        try{Request=_viewModel.CreateRequest(Decimal.ToInt32(_bidder.Value),Decimal.ToInt32(_quantity.Value),Decimal.ToInt32(_deals.Value),_price.Value,_reason.Text);DialogResult=DialogResult.OK;Close();}
        catch(Exception exception){MessageBox.Show(this,exception.Message,"Sale Correction",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }

    private static void Add(TableLayoutPanel table,string label,Control control,int row){table.RowStyles.Add(new RowStyle(SizeType.AutoSize));table.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,10,8,8),Font=new Font("Segoe UI",10.5F,FontStyle.Bold)},0,row);control.Dock=DockStyle.Fill;control.Margin=new Padding(0,6,0,6);table.Controls.Add(control,1,row);}
}
