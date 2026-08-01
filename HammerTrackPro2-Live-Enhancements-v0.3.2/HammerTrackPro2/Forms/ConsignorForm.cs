using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
namespace HammerTrackPro2.Forms;
public sealed class ConsignorForm : Form
{
    private readonly ConsignorViewModel _viewModel;
    private readonly PropertyGrid _editor = new() { Dock = DockStyle.Fill, HelpVisible = true, ToolbarVisible = false };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
    private readonly TextBox _search = new() { Width = 360, PlaceholderText = "Search consignors" };
    public ConsignorForm(ConsignorService service)
    {
        _viewModel = new ConsignorViewModel(service); Text="Consignor Management"; StartPosition=FormStartPosition.CenterParent; Width=1300; Height=800; MinimumSize=new Size(1000,650);
        var split=new SplitContainer { Dock=DockStyle.Fill, SplitterDistance=450, FixedPanel=FixedPanel.Panel1 };
        var left=new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=2 }; left.RowStyles.Add(new RowStyle(SizeType.Percent,100)); left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var buttons=new FlowLayoutPanel { Dock=DockStyle.Fill, AutoSize=true }; buttons.Controls.Add(Button("New", (_,_)=>New())); buttons.Controls.Add(Button("Save", (_,_)=>Save())); buttons.Controls.Add(Button("Delete", (_,_)=>Delete())); left.Controls.Add(_editor,0,0); left.Controls.Add(buttons,0,1);
        var right=new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=2 }; right.RowStyles.Add(new RowStyle(SizeType.Absolute,46)); right.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        _search.TextChanged += (_,_)=>{_viewModel.SearchText=_search.Text; _viewModel.Search(); RefreshGrid();}; _grid.SelectionChanged += (_,_)=>SelectConsignor(); right.Controls.Add(_search,0,0); right.Controls.Add(_grid,0,1); split.Panel1.Controls.Add(left); split.Panel2.Controls.Add(right); Controls.Add(split);
        _viewModel.Load(); New(); RefreshGrid();
    }
    private static Button Button(string text, EventHandler click) { var b=new Button { Text=text, AutoSize=true, Padding=new Padding(12,5,12,5) }; b.Click+=click; return b; }
    private void New() { _viewModel.BeginNew(); _editor.SelectedObject=_viewModel.Editor; _grid.ClearSelection(); }
    private void SelectConsignor() { if(_grid.CurrentRow?.DataBoundItem is not Consignor c)return; _viewModel.BeginEdit(c); _editor.SelectedObject=_viewModel.Editor; }
    private void RefreshGrid() { _grid.DataSource=null; _grid.DataSource=_viewModel.Consignors.ToList(); _grid.ClearSelection(); }
    private void Save() { try { _editor.Refresh(); _viewModel.Save(); RefreshGrid(); _editor.SelectedObject=_viewModel.Editor; } catch(Exception ex) { MessageBox.Show(ex.Message,"Consignor Validation",MessageBoxButtons.OK,MessageBoxIcon.Warning); } }
    private void Delete() { if(_grid.CurrentRow?.DataBoundItem is not Consignor c)return; if(MessageBox.Show($"Delete consignor {c.ConsignorNumber} - {c.DisplayName}?","Confirm Delete",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return; try {_viewModel.BeginEdit(c); _viewModel.Delete(); RefreshGrid(); _editor.SelectedObject=_viewModel.Editor;} catch(Exception ex){MessageBox.Show(ex.Message,"Delete Consignor",MessageBoxButtons.OK,MessageBoxIcon.Error);} }
}
