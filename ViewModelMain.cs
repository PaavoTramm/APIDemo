using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Buffers;

namespace ApiDemo
{
    public class ViewModelMain : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string URI { get; set; } = "https://api.woodston.ee/v1/";
        public string UserName
        {
            get { return Settings.Current.UserName; }
            set { Settings.Current.UserName = value; Settings.Instance.Save(); }
        }  
        public string Password
        {
            get { return Settings.Current.Password; }
            set { Settings.Current.Password = value; Settings.Instance.Save(); }
        }  
        public string ShapeFile
        {
            get { return Settings.Current.ShapeFile; }
            set { Settings.Current.ShapeFile = value; Settings.Instance.Save(); }
        }
        public string ScriptFile
        {
            get { return Settings.Current.ScriptFile; }
            set { Settings.Current.ScriptFile = value; Settings.Instance.Save(); }
        }  
        public string DataFile
        {
            get { return Settings.Current.DataFile; }
            set { Settings.Current.DataFile = value; Settings.Instance.Save(); }
        }  
        public string ResultFile
        {
            get { return Settings.Current.ResultFile; }
            set { Settings.Current.ResultFile = value; Settings.Instance.Save(); }
        }  
        
        StringBuilder log = new StringBuilder();
        public string Log 
        { 
            get { return log.ToString(); }
            set { log.Append(value); }
        }

        public ViewModelMain()
        {
            Settings.Instance.Load();

            if (ShapeFile == "" && ScriptFile == "" && DataFile == "" && ResultFile == "")
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                
                ShapeFile = Path.Combine(path, "Document Examples", "invoice.shape");
                ScriptFile = Path.Combine(path, "Document Examples", "invoice.js");
                DataFile = Path.Combine(path, "Document Examples", "invoice.xml");
                ResultFile = Path.Combine(path, "Document Examples", "generated.pdf");
            }
        }

        void MessageOutput(string message)
        {
            log.AppendLine(message);
            OnPropertyChanged("Log");
        }

        bool inProgress = false;
        public bool InProgress 
        {
            get { return inProgress; }
            set
            {
                inProgress = value;
                OnPropertyChanged();
            }
        }

        DocumentAPI? api;

        DelegateCommand? upload;
        public DelegateCommand Upload
        {
            get
            {
                if(upload == null)
                    upload = new DelegateCommand(DoUpload);
                return upload;
            }
        }

        DelegateCommand? run;
        public DelegateCommand Run
        {
            get
            {
                if (run == null)
                    run = new DelegateCommand(DoRun);
                return run;
            }
        }

        DelegateCommand? cancel;
        public DelegateCommand Cancel
        {
            get
            {
                if (cancel == null)
                    cancel = new DelegateCommand(DoCancel);
                return cancel;
            }
        }

        public async void DoUpload(object? parameter)
        {
            InProgress = true;
            try
            {
                await TestUpload();
            }
            finally
            {
                InProgress = false;
            }
        }

        public async void DoRun(object? parameter)
        {
            InProgress = true;
            try
            {
                await TestRun();
            }

            finally
            {
                InProgress = false;
            }
        }

        public async void DoCancel(object? parameter)
        {
            await Task.Delay(5);

            InProgress = false;
        }
        
        public async Task<bool> TestUpload()
        {
            if (api == null)
                api = new DocumentAPI(URI, UserName, Password);

            if (false == await api.Authenticate())
            {
                MessageOutput($"Failed to authenticate {UserName}");
                return false;
            }

            try
            {
                string shapeFile = Path.GetFileName(ShapeFile);
                string dataFile = Path.GetFileName(DataFile);

                List<DocumentAPI.Service>? services = await api.GetServices();
                if (services == null)
                    return false;

                DocumentAPI.Service? selected = services.FirstOrDefault(x => x.shape.Equals(shapeFile));
                if (null == selected)
                {
                    if (!File.Exists(ShapeFile))
                    {
                        MessageOutput($"Shape file does not exist");
                        return false;
                    }

                    DocumentAPI.Created? created = await api.CreateService(shapeFile);
                    if (null == created)
                    {
                        MessageOutput($"Failed to create service for {shapeFile}");
                        return false;
                    }

                    if (File.Exists(ShapeFile))
                        await api.SendResource(created.id, ShapeFile, "application/vnd.ws-doc");
                    if (File.Exists(ScriptFile))
                        await api.SendResource(created.id, ScriptFile, "text/javascript");
                    if (File.Exists(DataFile))
                        await api.SendResource(created.id, DataFile, "text/xml");

                    selected = await api.GetService(created.id);
                    if (selected != null)
                        services.Add(selected);

                    MessageOutput($"Service: {shapeFile} was uploaded");
                }
                else
                {
                    MessageOutput($"Service: {selected.shape} is already on server");
                }

                foreach (var s in services)
                    MessageOutput($"Service: {s.shape} : {s.id}");

                if (selected == null)
                    return false;

                List<DocumentAPI.Resource>? resources = await api.GetResources(selected.id);
                if (resources != null && resources.Count > 0)
                {
                    foreach (var resource in resources)
                    {
                        MessageOutput($"  Service {selected.id} has resource {resource.name}");

                        if (resource.name == dataFile)
                            await api.UpdateResource(selected.id, resource.id, DataFile, "text/xml");

                        if (resource.name == "test.xml")
                            await api.DeleteResource(selected.id, resource.id);
                    }
                }
            }

            catch(Exception ex)
            {
                MessageOutput(ex.Message);
                return false;
            }

            finally
            {
                await api.UnAuthenticate();

                api = null;
            }
            return true;
        }

        public async Task<bool> TestRun()
        {
            if (api == null)
                api = new DocumentAPI(URI, UserName, Password);

            if( false == await api.Authenticate() )
            {
                MessageOutput($"Failed to authenticate {UserName}");
                return false;
            }

            try
            {
                string shapeFile = Path.GetFileName(ShapeFile);
                string dataFile = Path.GetFileName(DataFile);
                string resultFile = Path.GetFileName(ResultFile);

                List<DocumentAPI.Service>? services = await api.GetServices();
                if (services == null)
                    return false;

                DocumentAPI.Service? selected = services.FirstOrDefault(x => x.shape.Equals(shapeFile));
                if (null == selected)
                    return false;

                if (selected == null)
                    return false;

                // optional: clear existing jobs

                List<DocumentAPI.Job>? jobs = await api.GetJobs(selected.id);

                if (jobs != null && jobs.Count > 0)
                {
                    foreach (var jrec in jobs)
                    {
                        await api.DeleteJob(selected.id, jrec.id);
                    }
                }

                // create a new job

                DocumentAPI.Created? job = null;
                {
                    job = await api.CreateJob(selected.id, resultFile, "application/pdf");
                    if (null == job)
                        return false;

                    MessageOutput($"Created job {job.id}");
                }

                MessageOutput($"Sending {DataFile}");

                if (false == await api.SendJobInput(selected.id, job.id, DataFile, "text/xml"))
                    return false;

                int retries = 0;
                do
                {
                    DocumentAPI.Job? record = await api.ReadJob(selected.id, job.id);
                    if (null == record)
                        return false;

                    if (record.status == "finished")
                        break;

                    if (record.status == "canceled")
                    {
                        MessageOutput($"Canceled: {job.id}");
                        return false;
                    }

                    if (InProgress == false)
                        return false;

                    MessageOutput($"Waiting");

                    await Task.Delay(1000);
                }
                while (true && ++retries < 100);

                if (retries >= 100)
                {
                    MessageOutput($"Giving up on service/job {selected.id}/{job.id} after max retries");
                    return false;
                }

                await api.ReadJobOutput(selected.id, job.id, ResultFile);

                MessageOutput($"Downloaded: {ResultFile}");

                await api.DeleteJob(selected.id, job.id);

                MessageOutput($"Deleted job: {job.id}");
            }

            catch (Exception ex)
            {
                MessageOutput(ex.Message);
                return false;
            }

            finally
            {
                await api.UnAuthenticate();

                api = null;
            }
            return true;
        }
    }
}
