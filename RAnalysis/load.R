loadraw<-function(doc,nodeNames = NULL){
   require(XML)
   require(data.table)
   nmspc<-c(nm = "Empty")
   loadedXml<-xmlParse(doc)
   exps<-getNodeSet(loadedXml,"//*/nm:Experiment",nmspc)  


   exParse <- function(exp){
                   conf<-getNodeSet(exp,"./nm:Configuration",nmspc)[[1]]
                   sm<-getNodeSet(exp,"./nm:Summary",nmspc)[[1]]
			 allAttrs<-c(xmlAttrs(conf),xmlAttrs(sm))
                   if (!is.null(nodeNames))
                   {
                     if (is.null(attr(nodeNames,"names"))) {
                       res<-lapply(nodeNames,
                               function(s)                                                              
                                 allAttrs[[s]]
                               ) 
                       attr(res,"names")=nodeNames
                     }
                     else {
                       res<-lapply(attr(nodeNames,"names"),
                               function(s)                                                             
                                 as(sub(",", ".",as.character(allAttrs[[s]]),fixed=TRUE),nodeNames[s])
                               )
                       attr(res,"names")=attr(nodeNames,"names")
                     }
                   }
                   else
                      res<-as.vector(allAttrs,mode="list")
                   res
               }
  

   configs<-lapply(exps,exParse)
   configs
}

loaddata<-function(doc,nodeNames=NULL){
  rbindlist(loadraw(doc,nodeNames))               
}


loaddataFromDir<-function(directory,nodeNames=NULL){
   files<-dir(directory,pattern="*.xml",full.names=TRUE)   
   lines<-unlist(lapply(files,function(x) loadraw(x,nodeNames)),recursive=FALSE)
   rbindlist(lines)
}