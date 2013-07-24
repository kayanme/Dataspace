keycountcheck<-function(data,weighted=FALSE){
   getMode<-function(n){
      case<-data$mode==n
      g<-round(data$gone[case]/ifelse(weighted,data$c2items[case],1),3)
      data.frame(
         keys=data$keys[case],
         gone=g,
         time=data$time[case],
         branch=data$branch[case])
   }
   none<-getMode("None")
   light<-getMode("Light")
   mixed<-getMode("Mixed")
   heavy<-getMode("Heavy")
 
   data.frame(
      keys=none$keys,
      branch=none$branch,
      none.gone=none$gone,
      mixed.gone=mixed$gone,  
      light.gone=light$gone,
      heavy.gone=heavy$gone,
      none.time=none$time,
      mixed.time=mixed$time,
      light.time=light$time,
      heavy.time=heavy$time
   )
}

relMisses<-function(matches){
      data.frame(
        keys=matches$keys,
        branch=matches$branch,
        none=matches$none.gone,
        light=round(matches$light.gone/matches$none.gone,3),
        mixed=round(matches$mixed.gone/matches$none.gone,3),
        heavy=round(matches$heavy.gone/matches$none.gone,3))
 
}

mainCorrelation<-function(data){#c количеством ушедших €чеек (data$gone)
   data$keys*data$c2items/(data$branch^2)
}

corrCalc<-function(vc,data,mode=NULL){
  if (is.null(mode))
      cor(vc,data$gone)
  else {
      f<-data$mode==mode
      cor(vc[f],data$gone[f])
  }
}




drawLight<-function(matches){
   require(rgl)
   rows<-seq(min(matches$keys),max(matches$keys),500)
   cols<-seq(min(matches$branch),max(matches$branch))
   persp3d(
       rows,
       cols,
       matrix(ifelse(matches$none>100,matches$light,1),ncol=length(cols)),
       xlab="Keys",
       ylab="Branch",
       zlab="Misses",
       col="green",
       back="lines")
}

drawMixed<-function(matches){
   require(rgl)
   rows<-seq(min(matches$keys),max(matches$keys),500)
   cols<-seq(min(matches$branch),max(matches$branch))
   persp3d(
       rows,
       cols,
       matrix(ifelse(matches$none>100,matches$mixed,1),ncol=length(cols)),
       xlab="Keys",
       ylab="Branch",
       zlab="Misses",
       col="green",
       back="lines")
}

drawHeavy<-function(matches){
   require(rgl)
   rows<-seq(min(matches$keys),max(matches$keys),500)
   cols<-seq(min(matches$branch),max(matches$branch))
   persp3d(
       rows,
       cols,
       matrix(ifelse(matches$none>100,matches$heavy,1),ncol=length(cols)),
       xlab="Keys",
       ylab="Branch",
       zlab="Misses",
       col="green",
       back="lines")
}