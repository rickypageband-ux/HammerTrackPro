using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class CashierBidderSearchForm : Form
{
    private readonly CashierBidderSearchViewModel _viewModel;
    private readonly TextBox _query=new(){Dock=DockStyle.Fill,PlaceholderText="Bidder number, name, or driver's-license number"};
    private readonly DataGridView _results=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,AllowUserToAddRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect};
    public int? SelectedBidderNumber { get; private set; }

    public CashierBidderSearchForm(CashierBidderSearchViewModel viewModel)
    {
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));
        Text="HammerTrack Pro — Search Registered Buyers";Width=760;Height=560;MinimumSize=new Size(600,420);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10F);
        _results.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(RegisteredBidderSummary.BuyerNumber),HeaderText="Bidder #",FillWeight=35});
        _results.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(RegisteredBidderSummary.Name),HeaderText="Registered Buyer",FillWeight=65});
        _results.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_results.RowHeadersVisible=false;
        _results.CellDoubleClick+=(_,_)=>SelectCurrent();_query.TextChanged+=(_,_)=>RefreshResults();_query.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){SelectCurrent();e.SuppressKeyPress=true;}};
        var searchRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Padding=new Padding(0,0,0,8)};searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));searchRow.Controls.Add(_query,0,0);var search=new Button{Text="Search",Dock=DockStyle.Fill};search.Click+=(_,_)=>RefreshResults();searchRow.Controls.Add(search,1,0);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var use=new Button{Text="Use Selected Buyer",Width=180,Height=42};use.Click+=(_,_)=>SelectCurrent();var cancel=new Button{Text="Cancel",Width=120,Height=42};cancel.Click+=(_,_)=>Close();actions.Controls.Add(cancel);actions.Controls.Add(use);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,Padding=new Padding(18)};layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,55));layout.Controls.Add(new Label{Text="Find an already registered buyer",AutoSize=true,Font=new Font("Segoe UI",14F,FontStyle.Bold)},0,0);layout.Controls.Add(searchRow,0,1);layout.Controls.Add(_results,0,2);layout.Controls.Add(actions,0,3);Controls.Add(layout);RefreshResults();
    }

    private void RefreshResults(){_results.DataSource=_viewModel.Search(_query.Text).ToList();if(_results.Rows.Count>0)_results.Rows[0].Selected=true;}
    private void SelectCurrent(){if(_results.CurrentRow?.DataBoundItem is not RegisteredBidderSummary bidder)return;SelectedBidderNumber=bidder.BuyerNumber;DialogResult=DialogResult.OK;Close();}
}
