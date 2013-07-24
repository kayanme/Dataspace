loadBranchData<-function(dir)
{
   source("load.R")
   source("preprocessModes.R")
   params<-c(
     KeyCount="integer",
     RebalanceActive="logical",
     MaxFixedCache2BranchLength = "integer",
     RebalancingMode = "character",
     Cache1Items= "double",
     Cache2Items= "double",
     Cache2ItemsGone = "double",
     AvgTime = "double",
     GoneIntensity="double"
   )    
   data<-loaddataFromDir(dir,params)
   processModes(data)   
}



parseBranchData<-function(data,collapse=TRUE){

  
   if (collapse){
    groups<-split(data,list(data$KeyCount,data$RebalancingMode,data$MaxFixedCache2BranchLength)) 
    cl<-lapply(groups,
      function(x)
         list(
            keys=x$KeyCount[1],
            mode=x$RebalancingMode[1],
            branch=x$MaxFixedCache2BranchLength[1],
            c1items=round(median(x$Cache1Items),0),
            c2items=round(median(x$Cache2Items),0),
            gone = round(median(x$Cache2ItemsGone),0),
		goneInt = round(median(x$GoneIntensity),2),
            time = round(median(x$AvgTime),2)
           ))
     rbindlist(cl)
   }
   else
     data.table(
            keys=data$KeyCount,
            mode=data$RebalancingMode,
            branch=data$MaxFixedCache2BranchLength,
            c1items=round(data$Cache1Items,0),
            c2items=round(data$Cache2Items,0),
            gone = round(data$Cache2ItemsGone,0),
		goneInt = round(data$GoneIntensity,2),
            time = round(data$AvgTime,2)
           )

  
}