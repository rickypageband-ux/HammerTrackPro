using HammerTrackPro2.Models;

namespace HammerTrackPro2.Forms;

public sealed class RefundForm : Form
{
    private readonly ComboBox _payment=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=420};
    private readonly NumericUpDown _amount=new(){DecimalPlaces=2,Maximum=100000000,Width=180};
    private readonly TextBox _reason=new(){Width=420};private readonly TextBox _reference=new(){Width=420};
    public RefundRequest? Request{get;private set;}
    public RefundForm(Invoice invoice)
    {
        Text="Record Buyer Refund";Width=560;Height=360;MinimumSize=new Size(520,340);StartPosition=FormStartPosition.CenterParent;
        var refundable=invoice.Payments.Select(p=>new Choice(p,Math.Max(0,p.AppliedAmount-p.ProcessingFee-invoice.Refunds.Where(r=>r.PaymentId==p.PaymentId).Sum(r=>r.Amount)))).Where(x=>x.Remaining>0).ToList();
        if(refundable.Count==0)throw new InvalidOperationException("This invoice has no refundable purchase payments.");_payment.DataSource=refundable;
        var table=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=2,RowCount=6,BackColor=Color.White};table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add(table,"Original payment",_payment,0);Add(table,"Refund amount",_amount,1);Add(table,"Reason",_reason,2);Add(table,"Reference",_reference,3);table.Controls.Add(new Label{Text="Card-processing fees are retained and cannot be included in the refund.",AutoSize=true,ForeColor=Color.DarkRed,Margin=new Padding(0,12,0,12)},0,4);table.SetColumnSpan(table.GetControlFromPosition(0,4)!,2);
        var buttons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};var save=new Button{Text="Record Refund",Width=150,Height=42};var cancel=new Button{Text="Cancel",Width=110,Height=42};save.Click+=(_,_)=>Save();cancel.Click+=(_,_)=>{DialogResult=DialogResult.Cancel;Close();};buttons.Controls.Add(save);buttons.Controls.Add(cancel);table.Controls.Add(buttons,1,5);Controls.Add(table);_payment.SelectedIndexChanged+=(_,_)=>SetMaximum();SetMaximum();
    }
    private void SetMaximum(){if(_payment.SelectedItem is Choice choice){_amount.Maximum=choice.Remaining;_amount.Value=choice.Remaining;}}
    private void Save(){if(_payment.SelectedItem is not Choice choice)return;if(string.IsNullOrWhiteSpace(_reason.Text)){MessageBox.Show(this,"Enter the reason for this refund.","Refund",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}Request=new RefundRequest{PaymentId=choice.Payment.PaymentId,Amount=_amount.Value,Reason=_reason.Text.Trim(),Reference=_reference.Text.Trim()};DialogResult=DialogResult.OK;Close();}
    private static void Add(TableLayoutPanel table,string label,Control control,int row){table.RowStyles.Add(new RowStyle(SizeType.AutoSize));table.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,10,8,8)},0,row);table.Controls.Add(control,1,row);}
    private sealed record Choice(Payment Payment,decimal Remaining){public override string ToString()=>$"{Payment.CreatedUtc.ToLocalTime():g} - {Payment.Method} - refundable {Remaining:C2}";}
}
