create view VwMyMetrics as
select top 1000 * from Metrics where userId=139283160
order by CreatedAt desc