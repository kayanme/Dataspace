run<-function(debug = FALSE)
{
   source("Load.R")
   if (debug) debug(loaddata)
   loaddata('exp1.xml',nodeNames=c(KeyCount="numeric",RebalanceActive="logical",RebalancingMode="character"))
}