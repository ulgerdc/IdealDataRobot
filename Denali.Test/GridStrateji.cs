using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

public class DatabaseHelper
{
    private string ConnectionString = "Data Source=.;Initial Catalog=robot;User ID=sa;Password=1;Connection Timeout=30;Min Pool Size=5;Max Pool Size=15;Pooling=true;TrustServerCertificate=True;";

    public SqlConnection GetConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public SqlDataReader ExecuteReader(string query)
    {
        var connection = GetConnection();
        SqlCommand command = new SqlCommand(query, connection);
        connection.Open();
        return command.ExecuteReader();
    }
}

public class GridBot
{
    private string ConnectionString = "Data Source=.;Initial Catalog=robot;User ID=sa;Password=1;Connection Timeout=30;Min Pool Size=5;Max Pool Size=15;Pooling=true;TrustServerCertificate=True;";

    private decimal profitPercentage = 0.05m; // Örnek kar oranı %5
    private string stockSymbol = "AAPL"; // Örnek hisse senedi sembolü

    public SqlConnection GetConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public decimal GetCurrentPrice(string stockSymbol)
    {
        return 10;
        // IdealData API'den anlık fiyat almak
        //var idealDataClient = new IdealDataClient(); // IdealData'nın sağlayacağı istemci nesnesi
        //return idealDataClient.GetStockPrice(stockSymbol); // Anlık fiyat döndürme
    }

    public List<dynamic> GetStocks()
    {
        using (var connection = GetConnection())
        {
            string query = "SELECT * FROM Stocks"; // Hisse senedi verilerini sorgulama
            var stocks = new List<dynamic>();

            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    stocks.Add(new
                    {
                        StockId = (int)reader["StockId"],
                        StockSymbol = (string)reader["StockSymbol"],
                        GridSpacing = (decimal)reader["GridSpacing"],
                        GridCount = (int)reader["GridCount"],
                        ProfitPercentage = (decimal)reader["ProfitPercentage"]
                    });
                }
            }
            return stocks;
        }
    }

    public List<dynamic> GetOpenPositions(int stockId)
    {
        using (var connection = GetConnection())
        {
            string query = "SELECT * FROM Positions WHERE StockId = @StockId AND Status = 'Open'";
            var positions = new List<dynamic>();

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StockId", stockId);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    positions.Add(new
                    {
                        PositionId = (int)reader["PositionId"],
                        Price = (decimal)reader["Price"],
                        Quantity = (decimal)reader["Quantity"]
                    });
                }
            }
            return positions;
        }
    }

    public void AddPosition(int stockId, decimal price, decimal quantity)
    {
        using (var connection = GetConnection())
        {
            string query = "INSERT INTO Positions (StockId, Price, Quantity, Status) VALUES (@StockId, @Price, @Quantity, 'Open')";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@StockId", stockId);
                command.Parameters.AddWithValue("@Price", price);
                command.Parameters.AddWithValue("@Quantity", quantity);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    public void ClosePosition(int positionId, decimal soldPrice)
    {
        using (var connection = GetConnection())
        {
            string query = "UPDATE Positions SET Status = 'Closed', SoldPrice = @SoldPrice WHERE PositionId = @PositionId";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PositionId", positionId);
                command.Parameters.AddWithValue("@SoldPrice", soldPrice);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    // Grid seviyelerini anlık fiyata göre hesapla
    public List<decimal> GenerateGridLevels(decimal currentPrice, decimal gridSpacing, int gridCount)
    {
        var levels = new List<decimal>();

        // Grid seviyeleri yukarı ve aşağı olmak üzere hesaplanır
        for (int i = 0; i < gridCount; i++)
        {
            // Aşağı ve yukarı seviyeler
            levels.Add(currentPrice - i * gridSpacing); // Aşağı seviyeler
            levels.Add(currentPrice + (i + 1) * gridSpacing); // Yukarı seviyeler
        }

        // Seviyeleri küçükten büyüğe sıralıyoruz
        return levels.OrderBy(level => level).ToList();
    }

    // Grid seviyelerinde pozisyon açma
    public void CheckGridLevels(int stockId, decimal gridSpacing, int gridCount, decimal currentPrice)
    {
        var gridLevels = GenerateGridLevels(currentPrice, gridSpacing, gridCount);
        var openPositions = GetOpenPositions(stockId);

        CheckBuyLevels(stockId, gridLevels, openPositions, currentPrice);
        CheckSellLevels(stockId, openPositions, currentPrice);
        AdjustGridIfNeeded(stockId, gridLevels, currentPrice, openPositions);
    }

    // Alım seviyelerini kontrol et
    private void CheckBuyLevels(int stockId, List<decimal> gridLevels, List<dynamic> openPositions, decimal currentPrice)
    {
        foreach (var level in gridLevels)
        {
            // Eğer mevcut fiyat belirli seviyelere düşerse ve o seviyede pozisyon yoksa, alım yap
            if (currentPrice <= level && !openPositions.Any(p => p.Price == level))
            {
                Console.WriteLine($"Alım yapılıyor: Seviye {level}, Fiyat {currentPrice}");
                AddPosition(stockId, level, 1); // 1 birim alım
                break;
            }
        }
    }

    // Satış seviyelerini kontrol et
    private void CheckSellLevels(int stockId, List<dynamic> openPositions, decimal currentPrice)
    {
        foreach (var position in openPositions)
        {
            decimal targetSellPrice = position.Price * (1 + profitPercentage);
            if (currentPrice >= targetSellPrice)
            {
                Console.WriteLine($"Satış yapılıyor: Pozisyon {position.PositionId}, Hedef Fiyat {targetSellPrice}, Mevcut Fiyat {currentPrice}");
                ClosePosition(position.PositionId, currentPrice);
            }
        }
    }

    // Grid seviyelerini ayarla (Zarar dağılımı ve yeni pozisyon açma)
    private void AdjustGridIfNeeded(int stockId, List<decimal> gridLevels, decimal currentPrice, List<dynamic> openPositions)
    {
        var maxPricePosition = openPositions.OrderByDescending(p => p.Price).FirstOrDefault();

        // Eğer en üstteki pozisyon kapanmışsa ve fiyat hala düşüyorsa, yeni pozisyon açma ve zarar dağıtma işlemi yap
        if (maxPricePosition != null && currentPrice < maxPricePosition.Price)
        {
            // En yüksek seviyedeki pozisyonu kapat
            ClosePosition(maxPricePosition.PositionId, currentPrice);

            // Zarar dağıtımı: En üst pozisyonun zararını diğer pozisyonlara yay
            decimal totalLoss = maxPricePosition.Price - currentPrice;
            decimal lossPerPosition = totalLoss / openPositions.Count;

            // Her pozisyonu ayarlayıp, yeni seviyede pozisyon aç
            foreach (var position in openPositions)
            {
                decimal adjustedPrice = position.Price - lossPerPosition;
                Console.WriteLine($"Pozisyon ayarlandı: Pozisyon {position.PositionId}, Yeni Fiyat {adjustedPrice}");
                ClosePosition(position.PositionId, adjustedPrice); // Eski pozisyonu kapat
                AddPosition(stockId, adjustedPrice, 1); // Yeni pozisyon aç
            }

            // Yeni bir alım pozisyonu aç
            AddPosition(stockId, currentPrice, 1); // Yeni pozisyon aç
        }
    }

    // Fiyat izleyici ve işlemci
    public void StartWatching()
    {
        var stocks = GetStocks();

        while (true)
        {
            foreach (var stock in stocks)
            {
                decimal currentPrice = GetCurrentPrice(stock.StockSymbol); // Anlık fiyatı al
                CheckGridLevels(stock.StockId, stock.GridSpacing, stock.GridCount, currentPrice);
            }

            Thread.Sleep(1000); // 1 saniyelik döngü
        }
    }
}


