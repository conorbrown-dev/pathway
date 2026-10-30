-- expect: Cities with employees but no contractors
SELECT City FROM Employees
UNION
SELECT City FROM Contractors
ORDER BY City;
