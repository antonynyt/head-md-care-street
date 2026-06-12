using System.Collections;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class DialogueDirectorBike : MonoBehaviour
{
    [System.Serializable]
    public class BikeSequence
    {
        // daysyarnode is an object containing a the yarn node name and 2 Gameobject references
        [System.Serializable]
        public class DayYarnNode
        {            
            public string nodeName;
            public GameObject[] objectsToActivate;
        }
        public DayYarnNode[] daysYarnNodes;
    }

    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float delayBetweenLoops = 7f;
    [SerializeField] private BikeSequence sequence;
    [SerializeField] private BikeBrake bikeBrake;

    private bool wasBraking = false;

    public static int CurrentDay { get; private set; } = 0;
    private string _currentNode;

    private void Awake()
    {
        CurrentDay = (CurrentDay) % (sequence.daysYarnNodes.Length) + 1;
        _currentNode = sequence.daysYarnNodes[CurrentDay - 1].nodeName;

        Debug.Log($"CurrentDay: {CurrentDay}, Node: {_currentNode}");
        ActivateObjectsForCurrentNode();

        dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        StartCoroutine(PlayLinesInOrder());
    }

    private void Update()
    {
        if (bikeBrake != null && bikeBrake.IsPressed)
        {
            wasBraking = true;
        }
        else if (wasBraking && !(bikeBrake != null && bikeBrake.HasReachedZeroSpeed))
        {
            wasBraking = false;
            StartCoroutine(PlayLinesInOrder());
        }
    }

    private IEnumerator PlayLinesInOrder()
    {
        yield return new WaitForSeconds(delayBetweenLoops);
        if (bikeBrake != null && bikeBrake.IsPressed)
        {
            yield break;
        }
        dialogueRunner.StartDialogue(_currentNode);
    }

    private void OnDialogueComplete()
    {
        if (bikeBrake != null && bikeBrake.IsPressed)
        {
            return;
        }
        StartCoroutine(PlayLinesInOrder());
    }

    private void OnDestroy()
    {
        dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    private void ActivateObjectsForCurrentNode()
    {
        //hide all other node objects first
        foreach (var dayNode in sequence.daysYarnNodes)
        {
            if (dayNode.nodeName != _currentNode)
            {
                foreach (var obj in dayNode.objectsToActivate)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }

            if (dayNode.nodeName == _currentNode)
            {
                foreach (var obj in dayNode.objectsToActivate)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }
        }
    }
}