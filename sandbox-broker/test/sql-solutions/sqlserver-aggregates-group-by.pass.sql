SELECT CustomerId, COUNT(*) AS OrderCount, SUM(Total) AS TotalSpent
FROM Orders
GROUP BY CustomerId
HAVING SUM(Total) >= 20
ORDER BY TotalSpent DESC;
