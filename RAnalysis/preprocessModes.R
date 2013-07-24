processModes<-function(data)
{
   attr(data$RebalancingMode,"levels") = c(attr(data$RebalancingMode,"levels"),"None")
   data$RebalancingMode[!data$RebalanceActive]="None"
   data
}