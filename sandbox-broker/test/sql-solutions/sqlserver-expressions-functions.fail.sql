-- expect: 5.00 is Standard, 20.00 is Premium
SELECT Name,
       UPPER(Name) + ' - ' + COALESCE(Category, 'Unsorted') AS Label,
       CASE WHEN Price <= 5 THEN 'Budget'
            WHEN Price <= 20 THEN 'Standard'
            ELSE 'Premium' END AS PriceBand
FROM Products;
