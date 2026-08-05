using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class ConsignorForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74);
    private static readonly Color Blue=Color.FromArgb(24,96,150);
    private static readonly Color Gold=Color.FromArgb(224,168,54);
    private static readonly Color Canvas=Color.FromArgb(242,245,248);

    private readonly ConsignorViewModel _viewModel;
    private readonly PropertyGrid _editor=new(){Dock=DockStyle.Fill,HelpVisible=true,ToolbarVisible=false,PropertySort=PropertySort.Categorized,BackColor=Color.White,ViewBackColor=Color.White,ViewForeColor=Color.FromArgb(35,45,55),CategoryForeColor=Color.FromArgb(24,50,74),LineColor=Color.FromArgb(220,226,232)};
    private readonly DataGridView _grid=new(){Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AllowUserToAddRows=false,AllowUserToDeleteRows=false};
    private readonly TextBox _search=new(){Width=360,PlaceholderText="Search number, name, phone, email, or Tax ID"};
    private readonly Label _status=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
    private readonly Label _count=new(){AutoSize=true,TextAlign=ContentAlignment.MiddleRight};
    private readonly Button _fullScreenButton=new(){Text="Full Screen"};
    private bool _fullScreen;

    public ConsignorForm(ConsignorService service)
    {
        _viewModel=new ConsignorViewModel(service);
        Text="HammerTrack Pro - Seller Management";
        StartPosition=FormStartPosition.CenterParent;
        Width=1380;
        Height=840;
        MinimumSize=new Size(1080,680);
        BackColor=Canvas;
        KeyPreview=true;

        ConfigureGrid();
        var addButton=Button("Add Seller",New,true);
        var saveButton=Button("Save Seller",Save);
        var deleteButton=Button("Delete Seller",Delete,false,true);

        var header=BuildHeader();
        var split=new SplitContainer{Size=new Size(1330,700),Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel1,SplitterWidth=8,Panel1MinSize=440,Panel2MinSize=480,SplitterDistance=500,BackColor=Color.FromArgb(205,214,222)};
        split.Panel1.Padding=new Padding(0,0,8,0);
        split.Panel2.Padding=new Padding(8,0,0,0);

        var editorTitle=SectionHeader("SELLER DETAILS","Complete the fields below, then choose Save Seller.");
        var editorButtons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Padding=new Padding(8,4,8,4),BackColor=Color.White};
        editorButtons.Controls.Add(addButton);editorButtons.Controls.Add(saveButton);editorButtons.Controls.Add(deleteButton);
        var left=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,BackColor=Color.White};
        left.RowStyles.Add(new RowStyle(SizeType.Absolute,62));left.RowStyles.Add(new RowStyle(SizeType.Percent,100));left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.Controls.Add(editorTitle,0,0);left.Controls.Add(_editor,0,1);left.Controls.Add(editorButtons,0,2);

        var listTitle=SectionHeader("SELLER DIRECTORY","Select a row to edit. Sensitive Tax ID values stay in the details panel.");
        var searchBand=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,Padding=new Padding(10,7,10,7),BackColor=Color.White};
        var searchLabel=new Label{Text="Search",AutoSize=true,Font=new Font("Segoe UI",10F,FontStyle.Bold),ForeColor=Navy,Margin=new Padding(0,9,8,0)};
        _search.Font=new Font("Segoe UI",11F);_search.Height=34;_search.Margin=new Padding(0,3,10,3);
        _count.Font=new Font("Segoe UI",10F,FontStyle.Bold);_count.ForeColor=Navy;_count.Margin=new Padding(8,9,0,0);
        searchBand.Controls.Add(searchLabel);searchBand.Controls.Add(_search);searchBand.Controls.Add(_count);
        var right=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,BackColor=Color.White};
        right.RowStyles.Add(new RowStyle(SizeType.Absolute,62));right.RowStyles.Add(new RowStyle(SizeType.Absolute,52));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        right.Controls.Add(listTitle,0,0);right.Controls.Add(searchBand,0,1);right.Controls.Add(_grid,0,2);
        split.Panel1.Controls.Add(left);split.Panel2.Controls.Add(right);

        var statusBar=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(14,4,14,4)};
        _status.Font=new Font("Segoe UI",9.5F,FontStyle.Bold);_status.ForeColor=Navy;statusBar.Controls.Add(_status);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(12,0,12,12),BackColor=Canvas};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,78));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        layout.Controls.Add(header,0,0);layout.Controls.Add(split,0,1);layout.Controls.Add(statusBar,0,2);Controls.Add(layout);

        _search.TextChanged+=(_,_)=>Search();
        _grid.SelectionChanged+=(_,_)=>SelectConsignor();
        _fullScreenButton.Click+=(_,_)=>ToggleFullScreen();
        KeyDown+=(_,e)=>HandleShortcut(e);
        _viewModel.Load();New();RefreshGrid();
    }

    private Control BuildHeader()
    {
        var panel=new Panel{Dock=DockStyle.Fill,BackColor=Navy,Padding=new Padding(20,10,16,10)};
        var brand=new Label{Text="HAMMERTRACK PRO",Dock=DockStyle.Left,Width=300,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",20F,FontStyle.Bold),ForeColor=Color.White};
        var subtitle=new Label{Text="SELLER MANAGEMENT",Dock=DockStyle.Left,Width=240,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",11F,FontStyle.Bold),ForeColor=Gold};
        StyleButton(_fullScreenButton,true);_fullScreenButton.Dock=DockStyle.Right;
        panel.Controls.Add(_fullScreenButton);panel.Controls.Add(subtitle);panel.Controls.Add(brand);return panel;
    }

    private static Control SectionHeader(string title,string subtitle)
    {
        var panel=new Panel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(14,8,14,4)};
        panel.Controls.Add(new Label{Text=subtitle,Dock=DockStyle.Bottom,Height=22,Font=new Font("Segoe UI",8.5F),ForeColor=Color.FromArgb(90,100,110)});
        panel.Controls.Add(new Label{Text=title,Dock=DockStyle.Top,Height=25,Font=new Font("Segoe UI",12F,FontStyle.Bold),ForeColor=Navy});
        return panel;
    }

    private static Button Button(string text,Action action,bool accent=false,bool danger=false)
    {
        var button=new Button{Text=text};StyleButton(button,accent,danger);button.Click+=(_,_)=>action();return button;
    }

    private static void StyleButton(Button button,bool accent=false,bool danger=false)
    {
        button.AutoSize=false;button.Width=142;button.Height=42;button.Margin=new Padding(5);button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;
        button.BackColor=danger?Color.FromArgb(166,48,48):accent?Gold:Blue;button.ForeColor=accent?Navy:Color.White;button.FlatAppearance.BorderColor=danger?Color.FromArgb(125,32,32):accent?Gold:Color.FromArgb(15,72,112);
        button.Font=new Font("Segoe UI",10F,FontStyle.Bold);button.Cursor=Cursors.Hand;
    }

    private void ConfigureGrid()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(Consignor.ConsignorNumber),HeaderText="Seller #",FillWeight=60});
        _grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(Consignor.DisplayName),HeaderText="Seller Name",FillWeight=150});
        _grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(Consignor.ContactName),HeaderText="Contact",FillWeight=115});
        _grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(Consignor.Phone),HeaderText="Phone",FillWeight=90});
        _grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=nameof(Consignor.Email),HeaderText="Email",FillWeight=130});
        _grid.Columns.Add(new DataGridViewCheckBoxColumn{DataPropertyName=nameof(Consignor.Active),HeaderText="Active",FillWeight=55});
        _grid.Columns.Add(new DataGridViewCheckBoxColumn{DataPropertyName=nameof(Consignor.HoldCheck),HeaderText="Hold",FillWeight=50});
        _grid.BackgroundColor=Color.White;_grid.BorderStyle=BorderStyle.None;_grid.RowHeadersVisible=false;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_grid.RowTemplate.Height=34;_grid.ColumnHeadersHeight=40;_grid.EnableHeadersVisualStyles=false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor=Navy;_grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;_grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",10F,FontStyle.Bold);
        _grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(214,232,245);_grid.DefaultCellStyle.SelectionForeColor=Navy;_grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(246,249,251);
    }

    private void HandleShortcut(KeyEventArgs e)
    {
        if(e.KeyCode==Keys.F11){ToggleFullScreen();e.Handled=true;return;}
        if(e.KeyCode==Keys.Escape&&_fullScreen){ToggleFullScreen();e.Handled=true;return;}
        if(e.Control&&e.KeyCode==Keys.N){New();e.SuppressKeyPress=true;return;}
        if(e.Control&&e.KeyCode==Keys.S){Save();e.SuppressKeyPress=true;}
    }

    private void ToggleFullScreen(){_fullScreen=!_fullScreen;FormBorderStyle=_fullScreen?FormBorderStyle.None:FormBorderStyle.Sizable;WindowState=_fullScreen?FormWindowState.Maximized:FormWindowState.Normal;_fullScreenButton.Text=_fullScreen?"Exit Full Screen":"Full Screen";}
    private void Search(){_viewModel.SearchText=_search.Text;_viewModel.Search();RefreshGrid();_status.Text=string.IsNullOrWhiteSpace(_search.Text)?"Showing all sellers.":$"Search results for '{_search.Text.Trim()}'.";}
    private void New(){_viewModel.BeginNew();_editor.SelectedObject=_viewModel.Editor;_grid.ClearSelection();_status.Text=$"Adding Seller #{_viewModel.Editor.ConsignorNumber}. Enter the seller details and choose Save Seller.";}
    private void SelectConsignor(){if(_grid.CurrentRow?.DataBoundItem is not Consignor seller)return;_viewModel.BeginEdit(seller);_editor.SelectedObject=_viewModel.Editor;_status.Text=$"Editing Seller #{seller.ConsignorNumber} - {seller.DisplayName}.";}
    private void RefreshGrid(){_grid.DataSource=null;_grid.DataSource=_viewModel.Consignors.ToList();_grid.ClearSelection();_count.Text=$"{_viewModel.Consignors.Count} seller{(_viewModel.Consignors.Count==1?string.Empty:"s")}";}
    private void Save()
    {
        try{_editor.Refresh();var number=_viewModel.Editor.ConsignorNumber;var name=_viewModel.Editor.DisplayName;_viewModel.Save();RefreshGrid();_editor.SelectedObject=_viewModel.Editor;_status.Text=$"Seller #{number} - {name} saved. Ready to add Seller #{_viewModel.Editor.ConsignorNumber}.";}
        catch(Exception exception){MessageBox.Show(exception.Message,"Seller Validation",MessageBoxButtons.OK,MessageBoxIcon.Warning);_status.Text="Seller was not saved. Correct the highlighted details and try again.";}
    }
    private void Delete()
    {
        if(_grid.CurrentRow?.DataBoundItem is not Consignor seller){MessageBox.Show("Select a seller from the directory before choosing Delete Seller.","Seller Management",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        if(MessageBox.Show($"Delete seller {seller.ConsignorNumber} - {seller.DisplayName}?","Confirm Delete",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        try{_viewModel.BeginEdit(seller);_viewModel.Delete();RefreshGrid();_editor.SelectedObject=_viewModel.Editor;_status.Text=$"Seller #{seller.ConsignorNumber} deleted.";}
        catch(Exception exception){MessageBox.Show(exception.Message,"Delete Seller",MessageBoxButtons.OK,MessageBoxIcon.Error);_status.Text="Seller could not be deleted.";}
    }
}
