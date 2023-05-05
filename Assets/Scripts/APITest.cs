using StudyStore;
using System.IO;
using UnityEngine;

public class APITest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        OnlineAPI api = new OnlineAPI();

        api.onLoginExpired += () =>
        {
            Debug.Log("login expired");
        };

        void errorHandler(string callName, APIError apiError) { 
            if(apiError.status == 0) {
                Debug.Log(callName + " : erreur réseau : " + apiError.message);
            } else if(apiError.status == 1)
            {
                Debug.Log(callName + " : erreur de lecture de fichier : " + apiError.message);
            } else if(apiError.status == 2)
            {
                Debug.Log(callName + " : erreur d'authentification : " + apiError.message);
            }
            else {
                Debug.Log(callName + " : erreur api :");
                Debug.Log("status : " + apiError.status);
                Debug.Log("title : " + apiError.title);
                Debug.Log("detail : " + apiError.detail);
            }
        }

        api.Login("test@test.com", "testtesttest", (user) =>
        {
            Debug.Log("success login");

            api.ListStudies((study) => true, (studies) =>
            {
                Debug.Log("we get " + studies.Length + " studies");

                Study studyToSelect = default(Study);

                foreach(Study study in studies)
                {
                    Debug.Log("study");
                    Debug.Log("uuid : " + study.uuid);
                    Debug.Log("title : " + study.title);
                    Debug.Log("type : " + study.type);

                    if(study.title == "CHU Vandoeuvre")
                    {
                        studyToSelect = study;
                    }
                }

                api.SelectStudy(studyToSelect, (selectedStudy) =>
                {
                    Debug.Log("selected study");
                    Debug.Log("uuid : " + selectedStudy.uuid);
                    Debug.Log("title : " + selectedStudy.title);
                    Debug.Log("type : " + selectedStudy.type);

                    api.CreateRandomAnonymizationId((randomAnonymizationId) =>
                    {
                        Debug.Log("random anonymization id created : " + randomAnonymizationId);

                        api.CreateSubject(randomAnonymizationId, (createdSubject) =>
                        {
                            Debug.Log("created subject");
                            Debug.Log("uuid : " + createdSubject.uuid);
                            Debug.Log("anonymizationId : " + createdSubject.anonymizationId);

                            api.SelectSubject(createdSubject.anonymizationId, (subject) =>
                            {
                                Debug.Log("selected subject");
                                Debug.Log("uuid : " + subject.uuid);
                                Debug.Log("anonymizationId : " + subject.anonymizationId);

                                api.CreateExperiment((createdExperiment) =>
                                {
                                    Debug.Log("created experiment");
                                    Debug.Log("uuid : " + createdExperiment.uuid);
                                    Debug.Log("timestamp : " + createdExperiment.timestamp);

                                    api.GetExperiment(createdExperiment.uuid, (experiment) =>
                                    {
                                        Debug.Log("get experiment");
                                        Debug.Log("uuid : " + experiment.uuid);
                                        Debug.Log("timestamp : " + experiment.timestamp);

                                        api.UploadFile(experiment, "unity-test-file.json", new StreamReader("C:\\Users\\martin\\Desktop\\feuille.txt"), () =>
                                        {
                                            Debug.Log("upload done");

                                            api.ListFiles(experiment, (expFileList) =>
                                            {
                                                Debug.Log("we get " + expFileList.Length + " files");

                                                ExpFile fileToRead = default(ExpFile);

                                                foreach(ExpFile file in expFileList) {
                                                    Debug.Log("file");
                                                    Debug.Log("name : " + file.filename);
                                                    Debug.Log("origin : " + file.origin.uuid);
                                                    Debug.Log("created : " + file.created);

                                                    if(file.filename == "unity-test-file.json")
                                                    {
                                                        fileToRead = file;
                                                    }
                                                }

                                                api.ReadFile(fileToRead, (streamReader) =>
                                                {
                                                    string content = streamReader.ReadToEnd();
                                                    Debug.Log("file content : <" + content + ">");

                                                    api.Logout(() =>
                                                    {
                                                        Debug.Log("success logout");
                                                    }, (error) => errorHandler("Logout", error));
                                                }, (error) => errorHandler("ReadFile", error));
                                            }, (error) => errorHandler("ListFiles", error));
                                        }, (error) => errorHandler("UploadFile", error));
                                    }, (error) => errorHandler("GetExperiment", error));
                                }, (error) => errorHandler("CreateExperiment", error));
                            }, (error) => errorHandler("SelectSubject", error));
                        }, (error) => errorHandler("CreateSubject", error));
                    }, (error) => errorHandler("CreateRandomAnonymizationId", error));
                }, (error) => errorHandler("SelectStudy", error));
            }, (error) => errorHandler("ListStudies", error));

            Debug.Log("end login");
        }, (error) => errorHandler("Login", error));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
