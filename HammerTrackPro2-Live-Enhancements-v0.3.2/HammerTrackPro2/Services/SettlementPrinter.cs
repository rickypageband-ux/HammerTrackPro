using System.Drawing.Printing;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class SettlementPrinter
{
    internal const string BuyerDeductionLabel = "Buyer balance deducted";
    public PrintDocument CreateDocument(ConsignorSettlement settlement,Auction auction)
    {
        ArgumentNullException.ThrowIfNull(settlement); ArgumentNullException.ThrowIfNull(auction);
        var document=new PrintDocument { DocumentName=$"Settlement {settlement.FormattedSettlementNumber}" };
        var nextLine=0;
        document.PrintPage+=(_,e)=>
        {
            nextLine=Draw(e.Graphics??throw new InvalidOperationException("The printer did not provide a drawing surface."),e.MarginBounds,settlement,auction,nextLine);
            e.HasMorePages=nextLine<settlement.Lots.Count; if(!e.HasMorePages)nextLine=0;
        };
        return document;
    }

    internal static int Draw(Graphics graphics,Rectangle bounds,ConsignorSettlement settlement,Auction auction,int startLine=0)
    {
        using var title=new Font("Segoe UI",18,FontStyle.Bold); using var heading=new Font("Segoe UI",10,FontStyle.Bold); using var body=new Font("Segoe UI",9); using var pen=new Pen(Color.Black);
        var y=bounds.Top; graphics.DrawString(auction.AuctionName,title,Brushes.Black,bounds.Left,y); y+=34;
        graphics.DrawString($"SELLER SETTLEMENT {settlement.FormattedSettlementNumber}",heading,Brushes.Black,bounds.Left,y); y+=24;
        graphics.DrawString($"Seller: #{settlement.ConsignorNumber}  {settlement.ConsignorName}",body,Brushes.Black,bounds.Left,y); y+=18;
        graphics.DrawString(settlement.ConsignorAddress,body,Brushes.Black,bounds.Left,y); y+=24; graphics.DrawLine(pen,bounds.Left,y,bounds.Right,y); y+=8;
        graphics.DrawString("Lot",heading,Brushes.Black,bounds.Left,y); graphics.DrawString("Description",heading,Brushes.Black,bounds.Left+60,y); graphics.DrawString("Hammer",heading,Brushes.Black,bounds.Right-90,y); y+=20;
        var availableRows=Math.Max(1,(bounds.Bottom-y-190)/18); var endLine=Math.Min(settlement.Lots.Count,startLine+availableRows);
        for(var index=startLine;index<endLine;index++)
        {
            var line=settlement.Lots[index];
            graphics.DrawString(line.LotNumber.ToString(),body,Brushes.Black,bounds.Left,y); graphics.DrawString(line.Description,body,Brushes.Black,new RectangleF(bounds.Left+60,y,bounds.Width-170,18)); graphics.DrawString(line.HammerPrice.ToString("C2"),body,Brushes.Black,bounds.Right-90,y); y+=18;
        }
        if(endLine<settlement.Lots.Count) { graphics.DrawString($"Continued — {settlement.Lots.Count-endLine} lot(s) remain",body,Brushes.Black,bounds.Left,y+12); return endLine; }
        y+=6; graphics.DrawLine(pen,bounds.Left,y,bounds.Right,y); y+=10;
        Total(graphics,body,bounds,ref y,"Hammer subtotal",settlement.HammerSubtotal); Total(graphics,body,bounds,ref y,$"Commission ({settlement.CommissionPercent:0.##}%)",settlement.CommissionAmount);
        Total(graphics,body,bounds,ref y,BuyerDeductionLabel,settlement.BuyerPurchaseBalanceDeduction);
        Total(graphics,heading,bounds,ref y,"Net proceeds",settlement.NetAmount); Total(graphics,body,bounds,ref y,"Payouts",settlement.AmountPaid); Total(graphics,heading,bounds,ref y,"Balance due",settlement.BalanceDue);
        y+=12; graphics.DrawString($"Status: {settlement.Status}",heading,Brushes.Black,bounds.Left,y);
        return endLine;
    }

    private static void Total(Graphics graphics,Font font,Rectangle bounds,ref int y,string label,decimal amount)
    { graphics.DrawString(label,font,Brushes.Black,bounds.Right-270,y); graphics.DrawString(amount.ToString("C2"),font,Brushes.Black,bounds.Right-90,y); y+=20; }
}
