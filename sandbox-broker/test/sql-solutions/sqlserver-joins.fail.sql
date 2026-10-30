-- expect: Every customer, with orders where they exist
SELECT c.Name, o.OrderId, o.Total
FROM Customers AS c
INNER JOIN Orders AS o ON o.CustomerId = c.CustomerId;
