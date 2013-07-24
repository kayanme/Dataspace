loadBranchData<-function(dir)
{
   source("load.R")
   source("preprocessModes.R")
   params<-c(
     KeyCount="integer",
     RebalanceActive="logical",
     RebalancingMode = "character",
     Cache2Items= "double",
     Cache2ItemsGone = "double",
     AvgTime = "double",
     GoneIntensity="double",
     BranchDepth="double",
     GoneIntensityForBranchDecrease="double",
     GoneIntensityForBranchIncrease="double"
   )    
   data<-loaddataFromDir(dir,params)
   processModes(data)   
}

parseBranchData<-function(data,collapse=TRUE){

   require(data.table)
   if (collapse){
    groups<-split(data,list(data$KeyCount,data$RebalancingMode,
                            data$GoneIntensityForBranchDecrease,
				    data$GoneIntensityForBranchIncrease)) 
    cl<-lapply(groups,
      function(x)
         list(
            keys=x$KeyCount[1],
            mode=x$RebalancingMode[1],                    
            dec=round(x$GoneIntensityForBranchDecrease[1],2),
            inc=round(x$GoneIntensityForBranchIncrease[1],2),
            c2items=round(median(x$Cache2Items),0),
            gone = round(median(x$Cache2ItemsGone),0),
		goneInt = round(median(x$GoneIntensity),2),
            time = round(median(x$AvgTime),2),
            branch=round(median(x$BranchDepth),0)          
           ))
     rbindlist(cl)
   }
   else
     data.table(
            keys=data$KeyCount,
            mode=data$RebalancingMode,
            dec=round(data$GoneIntensityForBranchDecrease,2),
            inc=round(data$GoneIntensityForBranchIncrease,2),
            c2items=round(data$Cache2Items,0),
            gone = round(data$Cache2ItemsGone,0),
		goneInt = round(data$GoneIntensity,2),
            time = round(data$AvgTime,2),
            branch=round(data$BranchDepth,0)
           )

  }
