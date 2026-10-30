SELECT Name, City
FROM Customers
WHERE Active = 1 AND (City <> 'London' OR City IS NULL)
ORDER BY CustomerId DESC;
