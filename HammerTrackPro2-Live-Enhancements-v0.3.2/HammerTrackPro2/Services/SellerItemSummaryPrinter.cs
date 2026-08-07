using System.Drawing.Printing;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class SellerItemSummaryPrinter
{
    public PrintDocument CreateDocument(ConsignorSettlement settlement,Auction auction)
    {
        ArgumentNullException.ThrowIfNull(settlement);ArgumentNullException.ThrowIfNull(auction);var rows=BuildRows(settlement);var document=new PrintDocument{DocumentName=$"Seller Item Summary {settlement.FormattedSettlementNumber}"};var next=0;document.PrintPage+=(_,e)=>{next=Draw(e.Graphics??throw new InvalidOperationException("The printer did not provide a drawing surface."),e.MarginBounds,settlement,auction,rows,next);e.HasMorePages=next<rows.Count;if(!e.HasMorePages)next=0;};return document;
    }

    internal static IReadOnlyList<SellerItemSummaryRow> BuildRows(ConsignorSettlement settlement)=>settlement.Lots.GroupBy(x=>new{x.LotNumber,x.Description,x.HammerPrice}).OrderBy(x=>x.Key.LotNumber).ThenBy(x=>x.Key.HammerPrice).Select(x=>new SellerItemSummaryRow(x.Key.LotNumber,x.Key.Description,x.Sum(y=>y.Quantity),x.Key.HammerPrice,x.Sum(y=>y.LineTotal))).ToList();
    internal static int TotalItemsSold(ConsignorSettlement settlement)=>settlement.Lots.Sum(x=>x.Quantity);

    internal static int Draw(Graphics graphics,Rectangle bounds,ConsignorSettlement settlement,Auction auction,IReadOnlyList<SellerItemSummaryRow> rows,int start=0)
    {
        using var title=new Font("Segoe UI",18,FontStyle.Bold);using var heading=new Font("Segoe UI",10,FontStyle.Bold);using var body=new Font("Segoe UI",9);using var totalFont=new Font("Segoe UI",11,FontStyle.Bold);using var pen=new Pen(Color.Black);using var right=new StringFormat{Alignment=StringAlignment.Far};var y=bounds.Top;
        graphics.DrawString(auction.CompanyDisplayName,title,Brushes.Black,bounds.Left,y);y+=34;graphics.DrawString($"SELLER ITEM SUMMARY {settlement.FormattedSettlementNumber}",heading,Brushes.Black,bounds.Left,y);y+=24;graphics.DrawString($"Seller: #{settlement.ConsignorNumber}  {settlement.ConsignorName}",body,Brushes.Black,bounds.Left,y);y+=18;graphics.DrawString(settlement.ConsignorAddress,body,Brushes.Black,bounds.Left,y);y+=24;graphics.DrawLine(pen,bounds.Left,y,bounds.Right,y);y+=8;
        var quantityX=bounds.Right-245;var priceX=bounds.Right-150;var totalX=bounds.Right-65;graphics.DrawString("Lot / Item",heading,Brushes.Black,bounds.Left,y);graphics.DrawString("Sold",heading,Brushes.Black,new RectangleF(quantityX,y,55,18),right);graphics.DrawString("Price Each",heading,Brushes.Black,new RectangleF(priceX-25,y,90,18),right);graphics.DrawString("Total",heading,Brushes.Black,new RectangleF(totalX-15,y,80,18),right);y+=22;
        var available=Math.Max(1,(bounds.Bottom-y-145)/20);var end=Math.Min(rows.Count,start+available);for(var i=start;i<end;i++){var row=rows[i];graphics.DrawString($"Lot {row.LotNumber} - {row.Description}",body,Brushes.Black,new RectangleF(bounds.Left,y,quantityX-bounds.Left-12,18));graphics.DrawString(row.Quantity.ToString(),body,Brushes.Black,new RectangleF(quantityX,y,55,18),right);graphics.DrawString(row.PriceEach.ToString("C2"),body,Brushes.Black,new RectangleF(priceX-25,y,90,18),right);graphics.DrawString(row.Total.ToString("C2"),body,Brushes.Black,new RectangleF(totalX-15,y,80,18),right);y+=20;}
        if(end<rows.Count){graphics.DrawString($"Continued - {rows.Count-end} row(s) remain",body,Brushes.Black,bounds.Left,y+12);return end;}y+=8;graphics.DrawLine(pen,bounds.Left,y,bounds.Right,y);y+=12;graphics.DrawString("TOTAL ITEMS SOLD",totalFont,Brushes.Black,bounds.Left,y);graphics.DrawString(TotalItemsSold(settlement).ToString(),totalFont,Brushes.Black,new RectangleF(bounds.Right-100,y,90,22),right);y+=26;graphics.DrawString("TOTAL HAMMER SALES",totalFont,Brushes.Black,bounds.Left,y);graphics.DrawString(settlement.HammerSubtotal.ToString("C2"),totalFont,Brushes.Black,new RectangleF(bounds.Right-130,y,120,22),right);return end;
    }
}

public sealed record SellerItemSummaryRow(int LotNumber,string Description,int Quantity,decimal PriceEach,decimal Total);
