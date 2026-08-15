using System.Drawing.Printing;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class SettlementPrinter
{
    internal const string DocumentHeading="SELLER SETTLEMENT";
    internal const string BuyerDeductionLabel="Buyer balance deducted";

    public PrintDocument CreateDocument(ConsignorSettlement settlement,Auction auction)
    {
        ArgumentNullException.ThrowIfNull(settlement);ArgumentNullException.ThrowIfNull(auction);
        var document=new PrintDocument{DocumentName=$"Settlement {settlement.FormattedSettlementNumber}"};var nextLine=0;
        document.PrintPage+=(_,e)=>{nextLine=Draw(e.Graphics??throw new InvalidOperationException("The printer did not provide a drawing surface."),e.MarginBounds,settlement,auction,nextLine);e.HasMorePages=nextLine<settlement.Lots.Count;if(!e.HasMorePages)nextLine=0;};return document;
    }

    internal static int Draw(Graphics graphics,Rectangle bounds,ConsignorSettlement settlement,Auction auction,int startLine=0)
    {
        using var heading=new Font("Segoe UI",9,FontStyle.Bold);using var body=new Font("Segoe UI",8.5F);using var small=new Font("Segoe UI",7.3F);using var totalFont=new Font("Segoe UI",10,FontStyle.Bold);using var line=new Pen(Color.FromArgb(190,200,210));using var navyBrush=new SolidBrush(PrintBranding.Navy);using var whiteBrush=new SolidBrush(Color.White);using var right=new StringFormat{Alignment=StringAlignment.Far};
        var y=PrintBranding.DrawHeader(graphics,bounds,auction,DocumentHeading,$"No. {settlement.FormattedSettlementNumber}");
        graphics.DrawString($"Auction: {auction.AuctionNumber} - {auction.AuctionName}",heading,navyBrush,bounds.Left,y);graphics.DrawString($"Date: {auction.AuctionDate:d}",body,Brushes.Black,bounds.Left,y+20);graphics.DrawString($"Location: {auction.Location}",body,Brushes.Black,bounds.Left+150,y+20);graphics.DrawString($"STATUS: {settlement.Status.ToUpperInvariant()}",heading,navyBrush,new RectangleF(bounds.Right-190,y,190,20),right);y+=48;
        PrintBranding.DrawSectionTitle(graphics,"SELLER INFORMATION",new Rectangle(bounds.Left,y,bounds.Width,26));y+=34;
        graphics.DrawString($"Seller #{settlement.ConsignorNumber}",heading,navyBrush,bounds.Left,y);graphics.DrawString(settlement.ConsignorName,heading,Brushes.Black,bounds.Left+105,y);y+=18;
        if(!string.IsNullOrWhiteSpace(settlement.ConsignorAddress)){graphics.DrawString(settlement.ConsignorAddress,body,Brushes.Black,bounds.Left,y);y+=18;}y+=5;

        var bidderX=bounds.Right-320;var quantityX=bounds.Right-245;var priceX=bounds.Right-155;var totalX=bounds.Right-70;
        graphics.FillRectangle(navyBrush,bounds.Left,y,bounds.Width,26);graphics.DrawString("LOT / DESCRIPTION",heading,whiteBrush,bounds.Left+8,y+5);graphics.DrawString("BIDDER #",heading,whiteBrush,new RectangleF(bidderX,y+5,65,18),right);graphics.DrawString("QUANTITY",heading,whiteBrush,new RectangleF(quantityX,y+5,70,18),right);graphics.DrawString("PRICE EACH",heading,whiteBrush,new RectangleF(priceX-15,y+5,90,18),right);graphics.DrawString("TOTAL",heading,whiteBrush,new RectangleF(totalX-10,y+5,70,18),right);y+=30;
        var availableRows=Math.Max(1,(bounds.Bottom-y-250)/20);var endLine=Math.Min(settlement.Lots.Count,startLine+availableRows);
        for(var index=startLine;index<endLine;index++)
        {
            var item=settlement.Lots[index];if((index-startLine)%2==1)graphics.FillRectangle(Brushes.WhiteSmoke,bounds.Left,y-1,bounds.Width,20);
            graphics.DrawString($"Lot {item.LotNumber} - {item.Description}",body,Brushes.Black,new RectangleF(bounds.Left+8,y,bidderX-bounds.Left-14,18));graphics.DrawString(item.BidderNumber.ToString(),body,Brushes.Black,new RectangleF(bidderX,y,65,18),right);graphics.DrawString(item.Quantity.ToString(),body,Brushes.Black,new RectangleF(quantityX,y,70,18),right);graphics.DrawString(item.HammerPrice.ToString("C2"),body,Brushes.Black,new RectangleF(priceX-15,y,90,18),right);graphics.DrawString(item.LineTotal.ToString("C2"),body,Brushes.Black,new RectangleF(totalX-10,y,70,18),right);y+=20;
        }
        if(endLine<settlement.Lots.Count){graphics.DrawLine(line,bounds.Left,y,bounds.Right,y);graphics.DrawString($"Continued - {settlement.Lots.Count-endLine} sale line(s) remain",heading,navyBrush,bounds.Left,y+12);return endLine;}

        graphics.DrawLine(line,bounds.Left,y,bounds.Right,y);y+=14;var detailTop=y;var leftWidth=bounds.Width/2-12;
        PrintBranding.DrawSectionTitle(graphics,"PAYOUT HISTORY",new Rectangle(bounds.Left,detailTop,leftWidth,26));var py=detailTop+33;
        if(settlement.Payouts.Count==0){graphics.DrawString("No recorded seller payouts.",body,Brushes.Gray,bounds.Left+8,py);py+=18;}else foreach(var payout in settlement.Payouts.Take(6)){var reference=string.IsNullOrWhiteSpace(payout.Reference)?string.Empty:$"  Ref: {payout.Reference}";graphics.DrawString($"{payout.CreatedUtc.ToLocalTime():g}  {payout.Method}{reference}",small,Brushes.Black,bounds.Left+8,py);graphics.DrawString(payout.Amount.ToString("C2"),body,Brushes.Black,new RectangleF(bounds.Left+leftWidth-95,py,85,17),right);py+=17;}
        var totalsX=bounds.Left+bounds.Width/2+8;var totalsWidth=bounds.Width/2-8;PrintBranding.DrawSectionTitle(graphics,"SETTLEMENT TOTALS",new Rectangle(totalsX,detailTop,totalsWidth,26));var ty=detailTop+33;
        DrawTotal(graphics,body,totalsX,totalsWidth,ref ty,"Hammer subtotal",settlement.HammerSubtotal);DrawTotal(graphics,body,totalsX,totalsWidth,ref ty,$"Commission ({settlement.CommissionPercent:0.##}%)",settlement.CommissionAmount);DrawTotal(graphics,body,totalsX,totalsWidth,ref ty,BuyerDeductionLabel,settlement.BuyerPurchaseBalanceDeduction);DrawTotal(graphics,totalFont,totalsX,totalsWidth,ref ty,"NET PROCEEDS",settlement.NetAmount);DrawTotal(graphics,body,totalsX,totalsWidth,ref ty,"Payouts",settlement.AmountPaid);DrawTotal(graphics,totalFont,totalsX,totalsWidth,ref ty,"BALANCE DUE",settlement.BalanceDue);
        using var footerFont=new Font("Segoe UI",7);graphics.DrawString("Generated by HammerTrack Pro",footerFont,Brushes.Gray,bounds.Left,bounds.Bottom-14);graphics.DrawString($"Printed {DateTime.Now:g}",footerFont,Brushes.Gray,new RectangleF(bounds.Right-180,bounds.Bottom-14,180,14),right);return endLine;
    }

    private static void DrawTotal(Graphics graphics,Font font,int x,int width,ref int y,string label,decimal amount)
    {using var right=new StringFormat{Alignment=StringAlignment.Far};graphics.DrawString(label,font,Brushes.Black,x+8,y);graphics.DrawString(amount.ToString("C2"),font,Brushes.Black,new RectangleF(x+width-105,y,95,18),right);y+=19;}
}
