using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common;

namespace Dataspace.Common.Services
{   
    public class TransitionStorage
    {

        private class DataRecord
        {
            public string Name;
            public Guid Id;
            public object Data;
        }

        [ThreadStatic]
        private static Stack<DataRecord> _dataStack;

        [ThreadStatic]
        private static Stack<DataRecord> _unactualStack;


        internal TransitionStorage()
        {

        }

        internal void PutMarkedObject(string name, Guid id)
        {
            if (_unactualStack == null)
                _unactualStack = new Stack<DataRecord>();

            _unactualStack.Push(new DataRecord {  Name = name, Id = id });
        }

        internal void ClearLastMarked()
        {
            if (_unactualStack == null)
                return;
            _unactualStack.Pop();
        }

        internal bool IsMarkedObject(string name, Guid id)
        {
            if (_unactualStack == null)
                return false;

            return _unactualStack.Any(k => k.Name == name && k.Id == id);
        }

        internal void PutObject(string name,Guid id,object data)
        {
            if (_dataStack ==null)
                _dataStack = new Stack<DataRecord>();

            _dataStack.Push(new DataRecord{Data = data,Name = name,Id = id});
        }

        internal void ClearObject()
        {
            if (_dataStack == null)
                return;

            _dataStack.Pop();
        }

        internal object FindOrReturnNullObject(string name,Guid id)
        {
            if (_dataStack == null)
                return null;

            return _dataStack.FirstOrDefault(k => k.Name == name && k.Id == id).ByDefault(k=>k.Data);
        }

       

        
    }
}
