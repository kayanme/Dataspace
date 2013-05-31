Feature: transacted notifications

Scenario: accepting notifications after post  
  Given [level] isolation level
  And a [distibuted] transaction
  When I post a resource
  Then there [afterpost] be an announce after post

  Examples:
    |    level        |distibuted |afterpost|
	|     Chaos       |   no      |should   | 
	| ReadUncommitted |   no      |should   |   
	| ReadCommitted   |   no      |shouldn't|       
	| RepeatableRead  |   no      |shouldn't|
	| Serializable    |   no      |shouldn't|  
    |   Snapshot      |   no      |shouldn't|
	|     Chaos       |   yes     |should   | 
    | ReadUncommitted |   yes     |should   |
	| ReadCommitted   |   yes     |should   |       
	| RepeatableRead  |   yes     |should   |
	| Serializable    |   yes     |should   |      
	 
  

Scenario: accepting notifications after transaction end
  Given [transactionMode] transaction mode 
  And [level] isolation level
  And a [distibuted] transaction
  When I post a resource
  Then there [afterend] be an announce after [transactionEnding]

Examples:
    |    level        |distibuted |transactionEnding|afterend |
	|     Chaos       |   no      |       commit    |shouldn't|
    |     Chaos       |   no      |      rollback   | should  |
	| ReadUncommitted |   no      |       commit    |shouldn't|
	| ReadUncommitted |   no      |      rollback   | should  |
	| ReadCommitted   |   no      |       commit    | should  |
	| ReadCommitted   |   no      |      rollback   |shouldn't|
	| RepeatableRead  |   no      |       commit    | should  |
	| RepeatableRead  |   no      |      rollback   |shouldn't|
	| Serializable    |   no      |       commit    | should  |
	| Serializable    |   no      |      rollback   |shouldn't|
    |   Snapshot      |   no      |       commit    | should  |
	|   Snapshot      |   no      |      rollback   |shouldn't|
	|     Chaos       |   yes     |       commit    |shouldn't|
	|     Chaos       |   yes     |      rollback   | should  |
    | ReadUncommitted |   yes     |       commit    |shouldn't|
	| ReadUncommitted |   yes     |      rollback   | should  |
	| ReadCommitted   |   yes     |       commit    |shouldn't|
	| ReadCommitted   |   yes     |      rollback   | should  |       
	| RepeatableRead  |   yes     |       commit    |shouldn't|
	| RepeatableRead  |   yes     |      rollback   | should  |
	| Serializable    |   yes     |       commit    |shouldn't|
	| Serializable    |   yes     |      rollback   | should  |    
