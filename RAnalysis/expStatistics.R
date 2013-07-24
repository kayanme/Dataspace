expStatistics<-function(data){
  require(data.table)
  groups<-split(data,list(data$KeyCount,data$RebalancingMode,data$MaxFixedCache2BranchLength)) 
  cl<-lapply(groups,
      function(x)
         list(
            keys=x$KeyCount[1],
            mode=x$RebalancingMode[1],
            branch=x$MaxFixedCache2BranchLength[1],
            avgGone = round(median(x$Cache2ItemsGone),0),
            avgTime =round( median(x$AvgTime),2),
            varGone = round(sd(x$Cache2ItemsGone),1),
            varTime = round(sd(x$AvgTime),2),
            wVarGone = round(sd(x$Cache2ItemsGone)/median(x$Cache2ItemsGone),3),
            wVarTime = round(sd(x$AvgTime)/median(x$AvgTime),2)
           ))
   rbindlist(cl)


}