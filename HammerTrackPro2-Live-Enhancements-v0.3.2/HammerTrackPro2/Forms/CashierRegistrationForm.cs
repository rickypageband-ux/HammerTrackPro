using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class CashierRegistrationForm : Form
{
    private readonly CashierRegistrationViewModel _viewModel;
    private readonly NumericUpDown _number=new(){Minimum=1,Maximum=999999};
    private readonly TextBox _business=new(),_first=new(),_last=new(),_phone=new(),_email=new();

    public AuctionBidder? Buyer=>_viewModel.Buyer;
    public Consignor? Consignor=>_viewModel.Seller;

    public CashierRegistrationForm(CashierRegistrationViewModel viewModel)
    {
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));
        Text=viewModel.IsSeller?"Register Seller":"Register Buyer";
        StartPosition=FormStartPosition.CenterParent;
        Width=viewModel.IsSeller?520:900;
        Height=viewModel.IsSeller?390:560;
        MinimumSize=viewModel.IsSeller?new Size(500,360):new Size(760,480);

        var editor=BuildEditor();
        if(viewModel.IsSeller) Controls.Add(editor);
        else
        {
            var split=new SplitContainer{Dock=DockStyle.Fill,SplitterDistance=420,Panel1MinSize=360,Panel2MinSize=300,Padding=new Padding(8)};
            split.Panel1.Controls.Add(editor);
            var grid=new DataGridView
            {
                Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
                RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                DataSource=viewModel.RegisteredBidders.ToList()
            };
            var right=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,Padding=new Padding(10)};
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            right.Controls.Add(new Label{Text=$"Registered Bidders - Current Auction ({viewModel.RegisteredBidders.Count})",AutoSize=true,Font=new Font("Segoe UI",11F,FontStyle.Bold),Margin=new Padding(0,0,0,8)},0,0);
            right.Controls.Add(grid,0,1);split.Panel2.Controls.Add(right);Controls.Add(split);
        }
    }

    private Control BuildEditor()
    {
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(18)};
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add(table,_viewModel.IsSeller?"Seller Number":"Buyer Number",_number);
        if(_viewModel.IsSeller)Add(table,"Business Name",_business);
        Add(table,"First Name",_first);Add(table,"Last Name",_last);Add(table,"Phone",_phone);Add(table,"Email",_email);
        var button=new Button{Text="Register and Select",AutoSize=true,Padding=new Padding(10,5,10,5)};button.Click+=(_,_)=>Save();
        table.Controls.Add(button,1,table.RowCount);return table;
    }

    private static void Add(TableLayoutPanel table,string label,Control control)
    {
        var row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.AutoSize));control.Dock=DockStyle.Fill;
        table.Controls.Add(new Label{Text=label,AutoSize=true,Anchor=AnchorStyles.Left},0,row);table.Controls.Add(control,1,row);
    }

    private void Save()
    {
        try
        {
            _viewModel.Create(Decimal.ToInt32(_number.Value),_business.Text,_first.Text,_last.Text,_phone.Text,_email.Text);
            DialogResult=DialogResult.OK;Close();
        }
        catch(InvalidOperationException exception){MessageBox.Show(this,exception.Message,"Registration",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
}
