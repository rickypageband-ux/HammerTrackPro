using System.Drawing.Printing;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class SellerItemSummaryPrinter
{
    internal const string DocumentHeading="SELLER ITEM SUMMARY";
    public PrintDocument CreateDocument(ConsignorSettlement settlement,Auction auction)
    {
        ArgumentNullException.ThrowIfNull(settlement);ArgumentNullException.ThrowIfNull(auction);var rows=BuildRows(settlement);var document=new PrintDocument{DocumentName=$"Seller Item Summary {settlement.FormattedSettlementNumber}"};var next=0;document.PrintPage+=(_,e)=>{next=Draw(e.Graphics??throw new InvalidOperationException("The printer did not provide a drawing surface."),e.MarginBounds,settlement,auction,rows,next);e.HasMorePages=next<rows.Count;if(!e.HasMorePages)next=0;};return document;
    }

    internal static IReadOnlyList<SellerItemSummaryRow> BuildRows(ConsignorSettlement settlement)=>settlement.Lots.GroupBy(x=>new{x.LotNumber,x.Description,x.BidderNumber,x.HammerPrice}).OrderBy(x=>x.Key.LotNumber).ThenBy(x=>x.Key.BidderNumber).ThenBy(x=>x.Key.HammerPrice).Select(x=>new SellerItemSummaryRow(x.Key.LotNumber,x.Key.Description,x.Key.BidderNumber,x.Sum(y=>y.Quantity),x.Key.HammerPrice,x.Sum(y=>y.LineTotal))).ToList();
    internal static int TotalItemsSold(ConsignorSettlement settlement)=>settlement.Lots.Sum(x=>x.Quantity);

    internal static int Draw(Graphics graphics,Rectangle bounds,ConsignorSettlement settlement,Auction auction,IReadOnlyList<SellerItemSummaryRow> rows,int start=0)
    {
        using var heading=new Font("Segoe UI",9,FontStyle.Bold);using var body=new Font("Segoe UI",8.5F);using var totalFont=new Font("Segoe UI",10,FontStyle.Bold);using var line=new Pen(Color.FromArgb(190,200,210));using var navyBrush=new SolidBrush(PrintBranding.Navy);using var whiteBrush=new SolidBrush(Color.White);using var right=new StringFormat{Alignment=StringAlignment.Far};
        var y=PrintBranding.DrawHeader(graphics,bounds,auction,DocumentHeading,$"No. {settlement.FormattedSettlementNumber}");
        graphics.DrawString($"Auction: {auction.AuctionNumber} - {auction.AuctionName}",heading,navyBrush,bounds.Left,y);graphics.DrawString($"Date: {auction.AuctionDate:d}",body,Brushes.Black,bounds.Left,y+20);graphics.DrawString($"Location: {auction.Location}",body,Brushes.Black,bounds.Left+150,y+20);y+=48;
        PrintBranding.DrawSectionTitle(graphics,"SELLER INFORMATION",new Rectangle(bounds.Left,y,bounds.Width,26));y+=34;graphics.DrawString($"Seller #{settlement.ConsignorNumber}",heading,navyBrush,bounds.Left,y);graphics.DrawString(settlement.ConsignorName,heading,Brushes.Black,bounds.Left+105,y);y+=18;if(!string.IsNullOrWhiteSpace(settlement.ConsignorAddress)){graphics.DrawString(settlement.ConsignorAddress,body,Brushes.Black,bounds.Left,y);y+=18;}y+=5;
        var bidderX=bounds.Right-320;var quantityX=bounds.Right-245;var priceX=bounds.Right-155;var totalX=bounds.Right-70;graphics.FillRectangle(navyBrush,bounds.Left,y,bounds.Width,26);graphics.DrawString("LOT / DESCRIPTION",heading,whiteBrush,bounds.Left+8,y+5);graphics.DrawString("BIDDER #",heading,whiteBrush,new RectangleF(bidderX,y+5,65,18),right);graphics.DrawString("QUANTITY",heading,whiteBrush,new RectangleF(quantityX,y+5,70,18),right);graphics.DrawString("PRICE EACH",heading,whiteBrush,new RectangleF(priceX-15,y+5,90,18),right);graphics.DrawString("TOTAL",heading,whiteBrush,new RectangleF(totalX-10,y+5,70,18),right);y+=30;
        var available=Math.Max(1,(bounds.Bottom-y-120)/20);var end=Math.Min(rows.Count,start+available);for(var i=start;i<end;i++){var row=rows[i];if((i-start)%2==1)graphics.FillRectangle(Brushes.WhiteSmoke,bounds.Left,y-1,bounds.Width,20);graphics.DrawString($"Lot {row.LotNumber} - {row.Description}",body,Brushes.Black,new RectangleF(bounds.Left+8,y,bidderX-bounds.Left-14,18));graphics.DrawString(row.BidderNumber.ToString(),body,Brushes.Black,new RectangleF(bidderX,y,65,18),right);graphics.DrawString(row.Quantity.ToString(),body,Brushes.Black,new RectangleF(quantityX,y,70,18),right);graphics.DrawString(row.PriceEach.ToString("C2"),body,Brushes.Black,new RectangleF(priceX-15,y,90,18),right);graphics.DrawString(row.Total.ToString("C2"),body,Brushes.Black,new RectangleF(totalX-10,y,70,18),right);y+=20;}
        if(end<rows.Count){graphics.DrawLine(line,bounds.Left,y,bounds.Right,y);graphics.DrawString($"Continued - {rows.Count-end} row(s) remain",heading,navyBrush,bounds.Left,y+12);return end;}graphics.DrawLine(line,bounds.Left,y,bounds.Right,y);y+=14;PrintBranding.DrawSectionTitle(graphics,"ITEM SUMMARY TOTALS",new Rectangle(bounds.Left,y,bounds.Width,26));y+=34;graphics.DrawString("TOTAL ITEMS SOLD",totalFont,Brushes.Black,bounds.Left+8,y);graphics.DrawString(TotalItemsSold(settlement).ToString(),totalFont,Brushes.Black,new RectangleF(bounds.Right-100,y,90,22),right);y+=24;graphics.DrawString("TOTAL HAMMER SALES",totalFont,Brushes.Black,bounds.Left+8,y);graphics.DrawString(settlement.HammerSubtotal.ToString("C2"),totalFont,Brushes.Black,new RectangleF(bounds.Right-130,y,120,22),right);using var footerFont=new Font("Segoe UI",7);graphics.DrawString("Generated by HammerTrack Pro",footerFont,Brushes.Gray,bounds.Left,bounds.Bottom-14);graphics.DrawString($"Printed {DateTime.Now:g}",footerFont,Brushes.Gray,new RectangleF(bounds.Right-180,bounds.Bottom-14,180,14),right);return end;
    }
}

public sealed record SellerItemSummaryRow(int LotNumber,string Description,int BidderNumber,int Quantity,decimal PriceEach,decimal Total);
