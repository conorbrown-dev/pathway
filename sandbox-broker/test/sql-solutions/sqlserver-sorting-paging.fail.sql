-- expect: ties must be ordered by Name
SELECT Name, Price
FROM Products
ORDER BY Price DESC
OFFSET 3 ROWS FETCH NEXT 3 ROWS ONLY;
