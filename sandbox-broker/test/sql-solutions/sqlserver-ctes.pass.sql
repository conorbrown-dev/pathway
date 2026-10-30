WITH CustomerTotals (CustomerId, Spent) AS (
    SELECT CustomerId, SUM(Total) FROM Orders GROUP BY CustomerId
)
SELECT c.Name, t.Spent
FROM CustomerTotals AS t
JOIN Customers AS c ON c.CustomerId = t.CustomerId
WHERE t.Spent > (SELECT AVG(Spent) FROM CustomerTotals)
ORDER BY t.Spent DESC;
