using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class AuctionSetupForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74);
    private static readonly Color Blue=Color.FromArgb(24,96,150);
    private static readonly Color Gold=Color.FromArgb(224,168,54);
    private static readonly Color Canvas=Color.FromArgb(242,245,248);

    private readonly AuctionSetupViewModel _viewModel;
    private readonly ComboBox _auctions=new(){DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top,Height=38};
    private readonly PropertyGrid _editor=new(){Dock=DockStyle.Fill,HelpVisible=true,ToolbarVisible=false,PropertySort=PropertySort.Categorized,BackColor=Color.White,ViewBackColor=Color.White,ViewForeColor=Color.FromArgb(35,45,55),CategoryForeColor=Color.FromArgb(24,50,74),LineColor=Color.FromArgb(220,226,232)};
    private readonly CheckBox _copyConsignors=new(){Text="Copy sellers into the duplicated auction",AutoSize=true};
    private readonly CheckBox _copyBidders=new(){Text="Copy registered bidders and bidder numbers",AutoSize=true};
    private readonly Label _status=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    private readonly Label _currentAuction=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    private readonly Button _fullScreenButton=new(){Text="Full Screen"};
    private bool _fullScreen;

    public AuctionSetupForm(AuctionSetupViewModel viewModel)
    {
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));
        Text="HammerTrack Pro - Auction Setup";
        Width=1240;Height=820;MinimumSize=new Size(980,680);StartPosition=FormStartPosition.CenterParent;BackColor=Canvas;KeyPreview=true;

        var header=BuildHeader();
        var split=new SplitContainer{Size=new Size(1190,680),Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel1,SplitterWidth=8,Panel1MinSize=330,Panel2MinSize=520,SplitterDistance=390,BackColor=Color.FromArgb(205,214,222)};
        split.Panel1.Padding=new Padding(0,0,8,0);split.Panel2.Padding=new Padding(8,0,0,0);

        var navigation=BuildNavigation();
        var editorHeader=SectionHeader("AUCTION DETAILS","Commercial terms, schedule, staff, and printed-document settings for the selected auction.");
        var saveBand=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8,4,8,4),BackColor=Color.White};
        saveBand.Controls.Add(Button("Save Auction",SaveAuction,true));
        var right=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,BackColor=Color.White};
        right.RowStyles.Add(new RowStyle(SizeType.Absolute,64));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.Controls.Add(editorHeader,0,0);right.Controls.Add(_editor,0,1);right.Controls.Add(saveBand,0,2);
        split.Panel1.Controls.Add(navigation);split.Panel2.Controls.Add(right);

        var statusBar=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(14,4,14,4)};
        _status.Font=new Font("Segoe UI",9.5F,FontStyle.Bold);_status.ForeColor=Navy;statusBar.Controls.Add(_status);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(12,0,12,12),BackColor=Canvas};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,78));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        layout.Controls.Add(header,0,0);layout.Controls.Add(split,0,1);layout.Controls.Add(statusBar,0,2);Controls.Add(layout);

        _fullScreenButton.Click+=(_,_)=>ToggleFullScreen();
        KeyDown+=(_,e)=>HandleShortcut(e);
        Load+=(_,_)=>{_viewModel.Load();RefreshEditor();_status.Text="Auction Setup ready.";};
    }

    private Control BuildHeader()
    {
        var panel=new Panel{Dock=DockStyle.Fill,BackColor=Navy,Padding=new Padding(20,10,16,10)};
        var brand=new Label{Text="HAMMERTRACK PRO",Dock=DockStyle.Left,Width=300,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",20F,FontStyle.Bold),ForeColor=Color.White};
        var subtitle=new Label{Text="AUCTION SETUP",Dock=DockStyle.Left,Width=190,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",11F,FontStyle.Bold),ForeColor=Gold};
        StyleButton(_fullScreenButton,true);_fullScreenButton.Dock=DockStyle.Right;panel.Controls.Add(_fullScreenButton);panel.Controls.Add(subtitle);panel.Controls.Add(brand);return panel;
    }

    private Control BuildNavigation()
    {
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=8,BackColor=Color.White,Padding=new Padding(16)};
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute,64));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,52));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(SectionHeader("AUCTION DIRECTORY","Select an auction, create a new one, or duplicate the selected auction."),0,0);
        panel.Controls.Add(new Label{Text="Saved Auctions",AutoSize=true,Font=new Font("Segoe UI",10F,FontStyle.Bold),ForeColor=Navy,Margin=new Padding(0,10,0,5)},0,1);
        _auctions.Font=new Font("Segoe UI",11F);panel.Controls.Add(_auctions,0,2);
        var actions=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=2,Margin=new Padding(0,10,0,0)};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        actions.Controls.Add(Button("New Auction",NewAuction,true),0,0);actions.Controls.Add(Button("Load Selected",LoadAuction),1,0);actions.Controls.Add(Button("Duplicate",DuplicateAuction),0,1);actions.Controls.Add(Button("Close Auction",CloseAuction,false,true),1,1);panel.Controls.Add(actions,0,3);
        _copyConsignors.Font=new Font("Segoe UI",10F,FontStyle.Bold);_copyConsignors.ForeColor=Navy;_copyConsignors.Margin=new Padding(4,14,4,10);panel.Controls.Add(_copyConsignors,0,4);
        _copyBidders.Font=new Font("Segoe UI",10F,FontStyle.Bold);_copyBidders.ForeColor=Navy;_copyBidders.Margin=new Padding(4,0,4,10);panel.Controls.Add(_copyBidders,0,5);
        var hint=new Label{Text="Tip: Duplicate starts a clean auction. Copy bidders keeps their existing bidder numbers, but never copies lots, sales, invoices, or payments.",Dock=DockStyle.Top,AutoSize=true,MaximumSize=new Size(330,0),Font=new Font("Segoe UI",9F),ForeColor=Color.FromArgb(75,85,95),Padding=new Padding(4,8,4,8)};panel.Controls.Add(hint,0,6);
        _currentAuction.Font=new Font("Segoe UI",10F,FontStyle.Bold);_currentAuction.ForeColor=Navy;_currentAuction.BackColor=Color.FromArgb(234,243,251);_currentAuction.Padding=new Padding(10);_currentAuction.Height=52;panel.Controls.Add(_currentAuction,0,7);
        return panel;
    }

    private static Control SectionHeader(string title,string subtitle)
    {
        var panel=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(0,6,0,4)};
        panel.Controls.Add(new Label{Text=subtitle,Dock=DockStyle.Bottom,Height=26,Font=new Font("Segoe UI",8.5F),ForeColor=Color.FromArgb(90,100,110)});
        panel.Controls.Add(new Label{Text=title,Dock=DockStyle.Top,Height=25,Font=new Font("Segoe UI",12F,FontStyle.Bold),ForeColor=Navy});return panel;
    }

    private static Button Button(string text,Action action,bool accent=false,bool danger=false){var button=new Button{Text=text};StyleButton(button,accent,danger);button.Click+=(_,_)=>Execute(action);return button;}
    private static void StyleButton(Button button,bool accent=false,bool danger=false){button.AutoSize=false;button.Dock=DockStyle.Fill;button.MinimumSize=new Size(132,42);button.Height=42;button.Margin=new Padding(5);button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;button.BackColor=danger?Color.FromArgb(166,48,48):accent?Gold:Blue;button.ForeColor=accent?Navy:Color.White;button.FlatAppearance.BorderColor=danger?Color.FromArgb(125,32,32):accent?Gold:Color.FromArgb(15,72,112);button.Font=new Font("Segoe UI",9.5F,FontStyle.Bold);button.Cursor=Cursors.Hand;}
    private void ToggleFullScreen(){_fullScreen=!_fullScreen;FormBorderStyle=_fullScreen?FormBorderStyle.None:FormBorderStyle.Sizable;WindowState=_fullScreen?FormWindowState.Maximized:FormWindowState.Normal;_fullScreenButton.Text=_fullScreen?"Exit Full Screen":"Full Screen";}
    private void HandleShortcut(KeyEventArgs e){if(e.KeyCode==Keys.F11){ToggleFullScreen();e.Handled=true;}else if(e.KeyCode==Keys.Escape&&_fullScreen){ToggleFullScreen();e.Handled=true;}else if(e.Control&&e.KeyCode==Keys.N){NewAuction();e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.S){SaveAuction();e.SuppressKeyPress=true;}}
    private void NewAuction(){_viewModel.NewAuction();RefreshEditor();_status.Text="Creating a new auction. Complete the details and choose Save Auction.";}
    private void DuplicateAuction(){_viewModel.DuplicatePreviousAuction();RefreshEditor();_status.Text="Auction settings duplicated. Enter a new Auction Number and Name, choose whether to copy sellers, then save.";}
    private void SaveAuction(){try{_viewModel.CopyConsignors=_copyConsignors.Checked;_viewModel.CopyBidders=_copyBidders.Checked;var saved=_viewModel.Save();RefreshEditor();_status.Text=$"Auction {saved.DisplayText} saved and selected.";}catch(Exception exception){ShowError(exception);}}
    private void LoadAuction(){if(_auctions.SelectedItem is not Auction auction){_status.Text="Select a saved auction before choosing Load Selected.";return;}try{_viewModel.LoadAuction(auction.AuctionId);RefreshEditor();_status.Text=$"Auction {auction.DisplayText} loaded and selected.";}catch(Exception exception){ShowError(exception);}}
    private void CloseAuction(){if(MessageBox.Show(this,"Close this auction? Operational changes will be locked and reopening is not currently available.","Close Auction",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;try{_viewModel.CloseAuction();RefreshEditor();_status.Text="Auction closed. Invoices remain available for viewing and printing.";}catch(Exception exception){ShowError(exception);}}
    private void RefreshEditor(){_auctions.DataSource=null;_auctions.DataSource=_viewModel.Auctions.ToList();_auctions.SelectedItem=_viewModel.Auctions.SingleOrDefault(x=>x.AuctionId==_viewModel.CurrentAuction.AuctionId);_editor.SelectedObject=_viewModel.CurrentAuction;_copyConsignors.Checked=_viewModel.CopyConsignors;_copyBidders.Checked=_viewModel.CopyBidders;var duplicating=_viewModel.CurrentAuction.AuctionId==0;_copyConsignors.Enabled=duplicating;_copyBidders.Enabled=duplicating;_currentAuction.Text=duplicating?"NEW AUCTION - not yet saved":$"CURRENT: {_viewModel.CurrentAuction.DisplayText} ({_viewModel.CurrentAuction.Status})";}
    private void ShowError(Exception exception){MessageBox.Show(exception.Message,"Auction Setup",MessageBoxButtons.OK,MessageBoxIcon.Error);_status.Text="The auction change was not saved. Correct the details and try again.";}
    private static void Execute(Action action){try{action();}catch(Exception exception){MessageBox.Show(exception.Message,"Auction Setup",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
}
