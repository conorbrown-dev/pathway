SELECT p.Name, p.Category, p.Price
FROM Products AS p
WHERE p.Price > (SELECT AVG(q.Price) FROM Products AS q WHERE q.Category = p.Category);
