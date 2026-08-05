using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class CashierRegistrationForm : Form
{
    private readonly CashierRegistrationViewModel _viewModel;
    private readonly NumericUpDown _number=new(){Minimum=1,Maximum=999999};
    private readonly TextBox _business=new(),_first=new(),_last=new(),_address=new(),_city=new(),_state=new(),_zip=new(),_phone=new(),_email=new(),_driversLicense=new();

    public AuctionBidder? Buyer=>_viewModel.Buyer;
    public Consignor? Consignor=>_viewModel.Seller;

    public CashierRegistrationForm(CashierRegistrationViewModel viewModel)
    {
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));
        Text=viewModel.IsSeller?"Register Seller":"Register Buyer";
        StartPosition=FormStartPosition.CenterParent;
        Width=920;
        Height=720;
        MinimumSize=new Size(780,500);
        BackColor=Color.FromArgb(242,245,248);

        var editor=BuildEditor();
        if(viewModel.RegisteredBidders.Count==0)Controls.Add(editor);
        else
        {
            var split=new SplitContainer{Size=new Size(860,500),Dock=DockStyle.Fill,Panel1MinSize=360,Panel2MinSize=300,SplitterDistance=420,Padding=new Padding(8)};
            split.Panel1.Controls.Add(editor);
            var grid=new DataGridView
            {
                Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
                RowHeadersVisible=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                DataSource=viewModel.RegisteredBidders.ToList()
            };
            grid.CellDoubleClick+=(_,_)=>{if(grid.CurrentRow?.DataBoundItem is RegisteredBidderSummary bidder)_number.Value=bidder.BuyerNumber;};
            var right=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,Padding=new Padding(10)};
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var listTitle=viewModel.IsSeller?"Existing Buyer Numbers — double-click to link":"Registered Buyers — Current Auction";
            right.Controls.Add(new Label{Text=$"{listTitle} ({viewModel.RegisteredBidders.Count})",AutoSize=true,Font=new Font("Segoe UI",11F,FontStyle.Bold),ForeColor=Color.FromArgb(24,50,74),Margin=new Padding(0,0,0,8)},0,0);
            right.Controls.Add(grid,0,1);split.Panel2.Controls.Add(right);Controls.Add(split);
        }
    }

    private Control BuildEditor()
    {
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(18),AutoScroll=true};
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add(table,_viewModel.IsSeller?"Seller Number":"Buyer Number",_number);
        if(_viewModel.IsSeller)Add(table,"Business Name",_business);
        Add(table,"First Name (optional)",_first);Add(table,"Last Name (optional)",_last);
        if(!_viewModel.IsSeller){Add(table,"Address (optional)",_address);Add(table,"City (optional)",_city);Add(table,"State (optional)",_state);Add(table,"ZIP (optional)",_zip);Add(table,"Phone Number (optional)",_phone);Add(table,"Email (optional)",_email);Add(table,"Driver's License Number (optional)",_driversLicense);}
        else{Add(table,"Phone",_phone);Add(table,"Email",_email);}
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
            _viewModel.Create(Decimal.ToInt32(_number.Value),_business.Text,_first.Text,_last.Text,_phone.Text,_email.Text,_address.Text,_city.Text,_state.Text,_zip.Text,_driversLicense.Text);
            DialogResult=DialogResult.OK;Close();
        }
        catch(InvalidOperationException exception){MessageBox.Show(this,exception.Message,"Registration",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
}
