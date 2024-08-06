using SimToolkit.ROS.Moveit;
using UnityEngine;
using UnityEngine.UIElements;

public class TrajectoryDatabaseView : MonoBehaviour
{   
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        await TrajectoryDatabase.AwaitLoad();

        var databaseContainer = new VisualElement();
        databaseContainer.AddToClassList("panel");
        databaseContainer.style.width = 300;
        databaseContainer.style.height = Length.Auto();
        databaseContainer.style.position = Position.Absolute;
        databaseContainer.style.left = 0;
        databaseContainer.style.bottom = 0;
        UIManager.rootElement.Add(databaseContainer);

        databaseContainer.Add(new Label("Trajectory Database"));

        var trajectoryListContainer = new ScrollView();
        trajectoryListContainer.style.flexGrow = 1;
        trajectoryListContainer.style.flexShrink = 1;
        trajectoryListContainer.style.maxHeight = 500;
        databaseContainer.Add(trajectoryListContainer);

        foreach (var trajectory in TrajectoryDatabase.AllTrajectories)
        {
            AddTrajectoryButton(trajectory, trajectoryListContainer);
        }

        var newTrajContainer = new VisualElement();
        newTrajContainer.style.flexDirection = FlexDirection.Row;
        newTrajContainer.style.justifyContent = Justify.SpaceBetween;
        newTrajContainer.style.maxWidth = Length.Percent(100);
        databaseContainer.Add(newTrajContainer);

        var newTrajNameField = new TextField();
        newTrajNameField.style.flexShrink = 1;
        newTrajNameField.style.flexGrow = 1;
        newTrajNameField.textEdition.placeholder = "New Path Name";
        newTrajContainer.Add(newTrajNameField);

        var newTrajButton = new Button(Background.FromVectorImage(Icons.GetVectorIcon("plus")));
        newTrajButton.text = "Path";
        newTrajButton.AddToClassList("button-text-icon");
        newTrajContainer.Add(newTrajButton);

        newTrajButton.clicked += () =>
        {
            TrajectoryDatabase.BeginTrajectory(newTrajNameField.value);
            AddTrajectoryButton(TrajectoryDatabase.CurrentTrajectory, trajectoryListContainer);
        };
    }

    void AddTrajectoryButton(TrajectoryInverseKinematic trajectory, VisualElement parent)
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;
        buttonContainer.style.maxWidth = Length.Percent(100);
        buttonContainer.style.minHeight = 35;
        parent.Add(buttonContainer);

        var trajButton = new Button();
        trajButton.text = trajectory.name;
        trajButton.style.height = 26;
        trajButton.style.flexGrow = 1;
        trajButton.style.flexShrink = 1;
        trajButton.clicked += () =>
        {
            TrajectoryDatabase.CurrentTrajectory = trajectory;

            // add the selected class to this button and remove it from the others
            parent.Query<Button>().ForEach(b => b.RemoveFromClassList("selected"));
            trajButton.AddToClassList("selected");
        };
        buttonContainer.Add(trajButton);

        // add two text fields for velocity and acceleration
        Debug.Log(trajectory.velAcc);
        if (trajectory.velAcc is float[] velAcc && velAcc.Length == 2)
        {
            var velocityField = new FloatField();
            var velocityInput = velocityField.Q("unity-text-input");

            velocityField.style.width = 35;
            velocityField.style.height = 26;
            velocityInput.style.paddingBottom = 1;
            velocityInput.style.paddingTop = 1;
            velocityInput.style.paddingLeft = 2;
            velocityInput.style.paddingRight = 2;
            velocityField.style.flexShrink = 1;
            velocityField.style.flexGrow = 0;
            velocityField.value = velAcc[0];

            velocityField.RegisterValueChangedCallback(evt =>
            {
                velAcc[0] = evt.newValue;
                TrajectoryDatabase.CurrentTrajectory = trajectory;
                TrajectoryDatabase.SaveTrajectory();
            });

            buttonContainer.Add(velocityField);

            var accelerationField = new FloatField();
            var accelerationInput = accelerationField.Q("unity-text-input");
            accelerationField.style.width = 35;
            accelerationField.style.height = 26;
            accelerationInput.style.paddingBottom = 1;
            accelerationInput.style.paddingTop = 1;
            accelerationInput.style.paddingLeft = 2;
            accelerationInput.style.paddingRight = 2;
            accelerationField.style.flexGrow = 0;
            accelerationField.value = velAcc[1];

            accelerationField.RegisterValueChangedCallback(evt =>
            {
                velAcc[1] = evt.newValue;
                TrajectoryDatabase.CurrentTrajectory = trajectory;
                TrajectoryDatabase.SaveTrajectory();
            });

            buttonContainer.Add(accelerationField);
        }

        // add a delete button to the trajectory button
        var deleteButton = new Button(Background.FromVectorImage(Icons.GetVectorIcon("trash")));
        buttonContainer.Add(deleteButton);
        deleteButton.style.marginRight = 3;
        deleteButton.style.marginTop = 3;
        deleteButton.style.width = 20;
        deleteButton.style.height = 20;
        deleteButton.style.paddingBottom = 1;
        deleteButton.style.paddingTop = 1;
        deleteButton.style.paddingLeft = 1;
        deleteButton.style.paddingRight = 1;
        deleteButton.style.backgroundColor = new Color (0.65f, 0.29f, 0.27f);
        deleteButton.clicked += () =>
        {
            TrajectoryDatabase.DeleteTrajectory(trajectory.name);
            trajButton.RemoveFromHierarchy();
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
