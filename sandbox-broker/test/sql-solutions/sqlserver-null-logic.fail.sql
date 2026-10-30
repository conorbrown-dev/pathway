-- expect: Linus
SELECT Name, City
FROM Customers
WHERE Active = 1 AND City <> 'London'
ORDER BY CustomerId DESC;
