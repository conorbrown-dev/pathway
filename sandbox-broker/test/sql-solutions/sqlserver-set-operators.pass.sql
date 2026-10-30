SELECT City FROM Employees
EXCEPT
SELECT City FROM Contractors
ORDER BY City;
