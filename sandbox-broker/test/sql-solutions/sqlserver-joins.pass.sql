SELECT c.Name, o.OrderId, o.Total
FROM Customers AS c
LEFT JOIN Orders AS o ON o.CustomerId = c.CustomerId;
