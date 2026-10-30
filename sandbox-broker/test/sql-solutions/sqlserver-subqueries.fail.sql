-- expect: equal to the average is not above it
SELECT Name, Category, Price
FROM Products
WHERE Price > (SELECT AVG(Price) FROM Products);
