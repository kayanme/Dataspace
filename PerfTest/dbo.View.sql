CREATE VIEW [dbo].[EventsView]
	AS SELECT [Time],[Key],ResourceType,[Length],'Cache' as Reason FROM CachedGetEvent WHERE CacheName = 'Client1'
UNION SELECT [Time],[Key],ResourceType,[Length],'External' FROM ExternalGetEvent WHERE CacheName = 'Client1'
UNION SELECT [Time],NEWID(),ResourceType,[Length],'Rebalance' FROM RebalanceEvent WHERE CacheName = 'Client1' AND Stage='Ended'
UNION SELECT [Time],[Key],ResourceType,'','Unactual' FROM UnactualGetEvent WHERE CacheName = 'Client1' 
