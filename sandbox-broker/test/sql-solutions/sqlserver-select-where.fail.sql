-- expect: 10.00 exactly counts
SELECT Name, Price
FROM Products
WHERE Category = 'Tea' AND Price > 10;
