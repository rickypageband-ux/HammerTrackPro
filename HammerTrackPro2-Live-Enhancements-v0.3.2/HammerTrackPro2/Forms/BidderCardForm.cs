using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class BidderCardForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74),Blue=Color.FromArgb(24,96,150),Gold=Color.FromArgb(224,168,54);
    private readonly BidderCardViewModel _viewModel;
    private readonly TextBox _search=new(){Width=330,PlaceholderText="Bidder number, name, or driver's license"};
    private readonly ComboBox _bidder=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=330,Font=new Font("Segoe UI",14F,FontStyle.Bold)};
    private readonly Label _auctionHouse=new(){AutoSize=true,Font=new Font("Segoe UI",11F,FontStyle.Bold),ForeColor=Navy};
    private readonly Label _status=new(){AutoSize=true,ForeColor=Color.DimGray};
    public BidderCardForm(BidderCardViewModel viewModel)
    {
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));Text="HammerTrack Pro — Bidder Cards";Width=760;Height=500;MinimumSize=new Size(680,440);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(242,245,248);
        var header=new Panel{Dock=DockStyle.Top,Height=78,BackColor=Navy,Padding=new Padding(22,10,18,10)};header.Controls.Add(new Label{Text="BIDDER CARDS",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",20F,FontStyle.Bold),ForeColor=Color.White});
        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Padding=new Padding(28,22,28,22),BackColor=Color.White};for(var i=0;i<7;i++)content.RowStyles.Add(new RowStyle(SizeType.AutoSize));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(new Label{Text="Print a full-page bidder card",AutoSize=true,Font=new Font("Segoe UI",16F,FontStyle.Bold),ForeColor=Navy},0,0);content.Controls.Add(new Label{Text="Search registered buyers, then choose the bidder card to print. Driver's-license numbers are searchable but are not displayed.",AutoSize=true,MaximumSize=new Size(650,0),Font=new Font("Segoe UI",10.5F),ForeColor=Color.DimGray,Margin=new Padding(0,6,0,12)},0,1);content.Controls.Add(new Label{Text="Search",AutoSize=true,Font=new Font("Segoe UI",10F,FontStyle.Bold),ForeColor=Navy},0,2);content.Controls.Add(_search,0,3);content.Controls.Add(new Label{Text="Bidder Number",AutoSize=true,Font=new Font("Segoe UI",10F,FontStyle.Bold),ForeColor=Navy,Margin=new Padding(0,10,0,2)},0,4);content.Controls.Add(_bidder,0,5);content.Controls.Add(_auctionHouse,0,6);content.Controls.Add(_status,0,7);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};actions.Controls.Add(ActionButton("Print Preview",PrintPreview,true));actions.Controls.Add(ActionButton("Refresh Bidders",RefreshBidders));actions.Controls.Add(ActionButton("Close",Close));content.Controls.Add(actions,0,8);Controls.Add(content);Controls.Add(header);_search.TextChanged+=(_,_)=>ApplyFilter();LoadBidders();
    }
    private void LoadBidders(){_viewModel.Refresh();ApplyFilter();_auctionHouse.Text=$"Printed auction house: {_viewModel.CurrentAuction.CompanyDisplayName}";}
    private void ApplyFilter(){var selected=_bidder.SelectedValue as int?;var rows=new CashierBidderSearchViewModel(_viewModel.RegisteredBidders).Search(_search.Text);_bidder.DataSource=rows.Select(x=>new BidderChoice(x.BuyerNumber,x.Name)).ToList();_bidder.DisplayMember=nameof(BidderChoice.Display);_bidder.ValueMember=nameof(BidderChoice.Number);if(selected is int number&&rows.Any(x=>x.BuyerNumber==number))_bidder.SelectedValue=number;_status.Text=rows.Count==0?"No registered bidders match this search.":$"{rows.Count} matching current-auction bidder(s).";}
    private void RefreshBidders()=>Execute(LoadBidders);
    private void PrintPreview()=>Execute(()=>{if(_bidder.SelectedValue is not int number)throw new InvalidOperationException("Select a bidder number to print.");_viewModel.SelectedBidderNumber=number;var selected=_viewModel.GetSelectedBidder();using var document=new BidderCardPrinter().CreateDocument(selected.BuyerNumber,_viewModel.CurrentAuction);PrinterPreferenceService.Apply(document);using var preview=new PrintPreviewDialog{Document=document,Text=$"Print Preview - Bidder Card #{selected.BuyerNumber}",Width=1000,Height=800,StartPosition=FormStartPosition.CenterParent,UseAntiAlias=true};preview.ShowDialog(this);});
    private static Button ActionButton(string text,Action action,bool accent=false){var button=new Button{Text=text,Width=175,Height=46,Margin=new Padding(0,0,10,0),FlatStyle=FlatStyle.Flat,BackColor=accent?Gold:Blue,ForeColor=accent?Navy:Color.White,Font=new Font("Segoe UI",10F,FontStyle.Bold),Cursor=Cursors.Hand};button.FlatAppearance.BorderColor=accent?Gold:Navy;button.Click+=(_,_)=>action();return button;}
    private static void Execute(Action action){try{action();}catch(Exception exception){MessageBox.Show(exception.Message,"Bidder Cards",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    private sealed record BidderChoice(int Number,string Name){public string Display=>$"#{Number} — {Name}";}
}
