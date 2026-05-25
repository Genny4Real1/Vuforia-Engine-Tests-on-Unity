using System;
using UnityEngine;
using Vuforia;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class VuforiaAndroidVoiceColor : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    [Header("Contenuto AR")]
    [SerializeField] private GameObject contenuto3D;

    [Header("Comandi vocali")]
    public string comandoCambioContrade = "cambia contrada";
    public string linguaRiconoscimento = "it-IT";

    [Header("Ascolto")]
    public bool ascoltoAutomatico = true;
    public float pausaTraAscolti = 0.8f;

    [Header("Contrade disponibili")]
    public Texture2D[] Contrade;

    private ObserverBehaviour observerBehaviour;
    private bool targetVisibile = false;
    private int indiceContrade = 0;
    private Renderer[] renderers;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject currentActivity;
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject recognizerIntent;
    private AndroidSpeechListener speechListener;

    private bool speechPronto = false;
    private bool speechInAscolto = false;
    private float prossimoAscolto = 0f;

    private readonly object speechLock = new object();
    private string testoRiconosciuto = null;
    private bool ascoltoTerminato = false;
#endif

    void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        }
        else
        {
            Debug.LogWarning("Nessun ObserverBehaviour trovato. Metti lo script sull'Image Target di Vuforia.");
        }

        if (contenuto3D != null)
        {
            renderers = contenuto3D.GetComponentsInChildren<Renderer>(true);
            contenuto3D.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Assegna il contenuto 3D nell'Inspector.");
        }
    }

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        RichiediPermessoMicrofono();
#else
        Debug.Log("Questo script usa il riconoscimento vocale Android. In Editor puoi premere C per testare il cambio colore.");
#endif
    }

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        GestisciRisultatiVocali();

        if (ascoltoAutomatico &&
            targetVisibile &&
            speechPronto &&
            !speechInAscolto &&
            Time.time >= prossimoAscolto)
        {
            AvviaAscolto();
        }
#else
        if (targetVisibile && Input.GetKeyDown(KeyCode.C))
        {
            CambiaContrada();
        }
#endif
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool riconosciuto =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED;

        targetVisibile = riconosciuto;

        if (contenuto3D != null)
        {
            contenuto3D.SetActive(targetVisibile);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!targetVisibile)
        {
            AnnullaAscolto();
        }
        else
        {
            prossimoAscolto = Time.time + 0.3f;
        }
#endif
    }

    private void CambiaContrada()
    {
        if (Contrade == null || Contrade.Length == 0)
        {
            Debug.LogWarning("Nessun colore disponibile.");
            return;
        }

        Texture2D nuovaTexture = Contrade[indiceContrade];
        ImpostaTexture(nuovaTexture);

        indiceContrade++;

        if (indiceContrade >= Contrade.Length)
        {
            indiceContrade = 0;
        }

        Debug.Log("Comando eseguito: cambia colore.");
    }

    private void ImpostaTexture(Texture2D texture)
    {
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("Nessun Renderer trovato nel contenuto 3D.");
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.materials)
            {
                if (material.HasProperty(BaseMapId))
                {
                    material.SetTexture(BaseMapId, texture);
                }
                else
                {
                    Debug.LogWarning("Il materiale non ha la proprietà URP _BaseMap: " + material.name);
                }
            }
        }
    }

    private void EseguiComandoVocale(string testo)
    {
        if (string.IsNullOrEmpty(testo))
            return;

        testo = testo.ToLowerInvariant().Trim();

        Debug.Log("Testo riconosciuto: " + testo);

        if (!targetVisibile)
        {
            Debug.Log("Comando ignorato: il target Vuforia non è visibile.");
            return;
        }

        if (testo.Contains(comandoCambioContrade))
        {
            CambiaContrada();
        }
        else if (testo.Contains("la flora"))
        {
            ImpostaTexture(Contrade[0]);
        }
        else if (testo.Contains("legnarello"))
        {
            ImpostaTexture(Contrade[1]);
        }
        else if (testo.Contains("san bernardino"))
        {
            ImpostaTexture(Contrade[2]);
        }
        else if (testo.Contains("san domenico"))
        {
            ImpostaTexture(Contrade[3]);
        }
        // else if (testo.Contains("san domenico"))
        // {
        //     ImpostaTexture(Contrade[3]);
        // }
        else if (testo.Contains("san magno"))
        {
            ImpostaTexture(Contrade[4]);
        }
        else if (testo.Contains("san martino"))
        {
            ImpostaTexture(Contrade[5]);
        }
        else if (testo.Contains("sant ambrogio"))
        {
            ImpostaTexture(Contrade[6]);
        }
        else if (testo.Contains("sant erasmo"))
        {
            ImpostaTexture(Contrade[7]);
        }

    }

#if UNITY_ANDROID && !UNITY_EDITOR

    private void RichiediPermessoMicrofono()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            InizializzaSpeechRecognizer();
            return;
        }

        PermissionCallbacks callbacks = new PermissionCallbacks();

        callbacks.PermissionGranted += permissionName =>
        {
            Debug.Log("Permesso microfono concesso.");
            InizializzaSpeechRecognizer();
        };

        callbacks.PermissionDenied += permissionName =>
        {
            Debug.LogWarning("Permesso microfono negato. Il comando vocale non può funzionare.");
        };

        callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
        {
            Debug.LogWarning("Permesso microfono negato definitivamente. Abilitalo dalle impostazioni Android.");
        };

        Permission.RequestUserPermission(Permission.Microphone, callbacks);
    }

    private void InizializzaSpeechRecognizer()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

        bool disponibile = speechClass.CallStatic<bool>("isRecognitionAvailable", currentActivity);

        if (!disponibile)
        {
            Debug.LogWarning("SpeechRecognizer non disponibile su questo dispositivo Android.");
            return;
        }

        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            AndroidJavaClass localSpeechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");

            speechRecognizer = localSpeechClass.CallStatic<AndroidJavaObject>(
                "createSpeechRecognizer",
                currentActivity
            );

            speechListener = new AndroidSpeechListener(this);

            speechRecognizer.Call("setRecognitionListener", speechListener);

            CreaRecognizerIntent();

            speechPronto = true;

            Debug.Log("SpeechRecognizer Android inizializzato.");
        }));
    }

    private void CreaRecognizerIntent()
    {
        AndroidJavaClass recognizerIntentClass = new AndroidJavaClass("android.speech.RecognizerIntent");

        string actionRecognizeSpeech =
            recognizerIntentClass.GetStatic<string>("ACTION_RECOGNIZE_SPEECH");

        string extraLanguageModel =
            recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE_MODEL");

        string languageModelFreeForm =
            recognizerIntentClass.GetStatic<string>("LANGUAGE_MODEL_FREE_FORM");

        string extraLanguage =
            recognizerIntentClass.GetStatic<string>("EXTRA_LANGUAGE");

        string extraMaxResults =
            recognizerIntentClass.GetStatic<string>("EXTRA_MAX_RESULTS");

        string extraPartialResults =
            recognizerIntentClass.GetStatic<string>("EXTRA_PARTIAL_RESULTS");

        recognizerIntent = new AndroidJavaObject(
            "android.content.Intent",
            actionRecognizeSpeech
        );

        recognizerIntent.Call<AndroidJavaObject>(
            "putExtra",
            extraLanguageModel,
            languageModelFreeForm
        );

        recognizerIntent.Call<AndroidJavaObject>(
            "putExtra",
            extraLanguage,
            linguaRiconoscimento
        );

        recognizerIntent.Call<AndroidJavaObject>(
            "putExtra",
            extraMaxResults,
            3
        );

        recognizerIntent.Call<AndroidJavaObject>(
            "putExtra",
            extraPartialResults,
            false
        );
    }

    private void AvviaAscolto()
    {
        if (!speechPronto || speechRecognizer == null || recognizerIntent == null)
            return;

        if (speechInAscolto)
            return;

        speechInAscolto = true;

        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                speechRecognizer.Call("startListening", recognizerIntent);
                Debug.Log("Ascolto vocale avviato.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Errore avvio SpeechRecognizer: " + e.Message);
                SegnalaFineAscolto();
            }
        }));
    }

    private void AnnullaAscolto()
    {
        if (!speechInAscolto || speechRecognizer == null || currentActivity == null)
            return;

        speechInAscolto = false;

        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                speechRecognizer.Call("cancel");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Errore cancel SpeechRecognizer: " + e.Message);
            }
        }));
    }

    private void GestisciRisultatiVocali()
    {
        string testoDaEseguire = null;
        bool fineAscolto = false;

        lock (speechLock)
        {
            if (!string.IsNullOrEmpty(testoRiconosciuto))
            {
                testoDaEseguire = testoRiconosciuto;
                testoRiconosciuto = null;
            }

            if (ascoltoTerminato)
            {
                fineAscolto = true;
                ascoltoTerminato = false;
            }
        }

        if (!string.IsNullOrEmpty(testoDaEseguire))
        {
            EseguiComandoVocale(testoDaEseguire);
        }

        if (fineAscolto)
        {
            speechInAscolto = false;
            prossimoAscolto = Time.time + pausaTraAscolti;
        }
    }

    public void RiceviRisultatoAndroid(string testo)
    {
        lock (speechLock)
        {
            testoRiconosciuto = testo;
            ascoltoTerminato = true;
        }
    }

    public void RiceviErroreAndroid(int codiceErrore)
    {
        Debug.Log("Errore SpeechRecognizer Android: " + codiceErrore);

        lock (speechLock)
        {
            ascoltoTerminato = true;
        }
    }

    private void SegnalaFineAscolto()
    {
        lock (speechLock)
        {
            ascoltoTerminato = true;
        }
    }

    private class AndroidSpeechListener : AndroidJavaProxy
    {
        private VuforiaAndroidVoiceColor controller;

        public AndroidSpeechListener(VuforiaAndroidVoiceColor controller)
            : base("android.speech.RecognitionListener")
        {
            this.controller = controller;
        }

        public void onReadyForSpeech(AndroidJavaObject parameters)
        {
            Debug.Log("Pronto per ascoltare.");
        }

        public void onBeginningOfSpeech()
        {
            Debug.Log("Inizio parlato.");
        }

        public void onRmsChanged(float rmsdB)
        {
        }

        public void onBufferReceived(byte[] buffer)
        {
        }

        public void onEndOfSpeech()
        {
            Debug.Log("Fine parlato.");
        }

        public void onError(int error)
        {
            controller.RiceviErroreAndroid(error);
        }

        public void onResults(AndroidJavaObject results)
        {
            string testo = EstraiRisultato(results);

            Debug.Log("Risultato vocale: " + testo);

            controller.RiceviRisultatoAndroid(testo);
        }

        public void onPartialResults(AndroidJavaObject partialResults)
        {
        }

        public void onEvent(int eventType, AndroidJavaObject parameters)
        {
        }

        private string EstraiRisultato(AndroidJavaObject bundle)
        {
            if (bundle == null)
                return "";

            AndroidJavaClass speechClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
            string resultsKey = speechClass.GetStatic<string>("RESULTS_RECOGNITION");

            AndroidJavaObject matches = bundle.Call<AndroidJavaObject>(
                "getStringArrayList",
                resultsKey
            );

            if (matches == null)
                return "";

            int count = matches.Call<int>("size");

            if (count <= 0)
                return "";

            return matches.Call<string>("get", 0);
        }
    }

#endif

    void OnDestroy()
    {
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (speechRecognizer != null && currentActivity != null)
        {
            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    speechRecognizer.Call("destroy");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Errore destroy SpeechRecognizer: " + e.Message);
                }
            }));
        }
#endif
    }
}