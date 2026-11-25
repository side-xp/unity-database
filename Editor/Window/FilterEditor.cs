using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace SideXP.Database.EditorOnly
{

    /// <summary>
    /// Edits a filter tree (see <see cref="FilterNode"/>): its groups, with their operator, and their conditions, each on a column of
    /// the table.
    /// </summary>
    /// <remarks>
    /// The editor changes the tree in place, and raises <see cref="Changed"/> after each change, so its owner applies the filter right
    /// away. The structure of the tree (nodes added, removed or replaced) rebuilds the elements, while the values (a typed text, a
    /// picked operator) only change their node, so a field keeps the focus while the user types.
    /// <para>
    /// A condition on a column that doesn't exist anymore, or whose values are of a type it doesn't support, stays in the tree and is
    /// shown as ignored, so the user can remove it or pick another column.
    /// </para>
    /// </remarks>
    internal class FilterEditor : VisualElement
    {

        #region Fields

        private const string EditorClass = "database-filter-editor";
        private const string GroupClass = "database-filter-group";
        private const string RootGroupClass = "database-filter-group--root";
        private const string GroupHeaderClass = "database-filter-group__header";
        private const string GroupOperatorClass = "database-filter-group__operator";
        private const string GroupNodesClass = "database-filter-group__nodes";
        private const string GroupFooterClass = "database-filter-group__footer";
        private const string GroupEmptyClass = "database-filter-group__empty";
        private const string ConditionClass = "database-filter-condition";
        private const string ConditionColumnClass = "database-filter-condition__column";
        private const string ConditionColumnMissingClass = "database-filter-condition__column--missing";
        private const string ConditionFieldClass = "database-filter-condition__field";
        private const string ConditionIgnoredClass = "database-filter-condition__ignored";
        private const string RemoveButtonClass = "database-filter-editor__remove";
        private const string AddButtonClass = "database-filter-editor__add";

        private const string AllOfText = "All of";
        private const string AnyOfText = "Any of";
        private const string OperatorTooltip = "All of: a row must meet every condition of the group. Any of: a row must meet at least " +
            "one of them.";
        private const string EmptyRootText = "No condition: all the rows are shown.";
        private const string EmptyGroupText = "Empty group, ignored.";
        private const string AddConditionText = "Condition";
        private const string AddConditionTooltip = "Adds a condition on a column to this group.";
        private const string AddGroupText = "Group";
        private const string AddGroupTooltip = "Adds a group of conditions to this group, combined with their own operator. For " +
            "example: Tier is Rare, and (Price below 100 or Weight below 5).";
        private const string RemoveConditionTooltip = "Removes this condition.";
        private const string RemoveGroupTooltip = "Removes this group and its conditions.";
        private const string ColumnTooltip = "The column of the condition. Click to pick another one.";
        private const string HiddenColumnSuffix = " (hidden)";
        private const string IgnoredText = "Ignored: the condition doesn't apply to this column.";
        private const string NotEditableText = "This condition can't be edited yet.";
        private const string TextPlaceholder = "Contains...";
        private const string TextTooltip = "Keeps the rows whose cell contains this text, ignoring case, as the cell is displayed " +
            "(names for enums, paths for references). Empty: all the rows.";
        private const string CheckedText = "is checked";
        private const string UncheckedText = "is unchecked";

        /// <summary>
        /// The choices of the operator of a group, in the order of <see cref="FilterOperator"/>.
        /// </summary>
        private static readonly List<string> OperatorChoices = new List<string> { AllOfText, AnyOfText };

        /// <summary>
        /// The choices of a checkbox condition: checked first, like <see cref="BoolCondition.IsChecked"/>.
        /// </summary>
        private static readonly List<string> CheckedChoices = new List<string> { CheckedText, UncheckedText };

        /// <summary>
        /// The edited tree, whose root is always a group.
        /// </summary>
        private FilterGroup _root = new FilterGroup();

        /// <summary>
        /// The columns that conditions can use, in their display order.
        /// </summary>
        private IReadOnlyList<TableViewColumn> _columns = Array.Empty<TableViewColumn>();

        /// <summary>
        /// Checks if a column is hidden in the table, so the menus can say it, or null if no column is hidden.
        /// </summary>
        private Predicate<string> _isColumnHidden = null;

        /// <summary>
        /// Called after each change of the tree, of its structure or of a value.
        /// </summary>
        public event Action Changed;

        #endregion


        #region Lifecycle

        /// <inheritdoc cref="FilterEditor"/>
        public FilterEditor()
        {
            AddToClassList(EditorClass);
            Rebuild();
        }

        #endregion


        #region Public API

        /// <inheritdoc cref="_root"/>
        public FilterGroup Root => _root;

        /// <summary>
        /// Edits a filter tree.
        /// </summary>
        /// <param name="root">The root of the tree to edit, changed in place, or null to start with an empty group.</param>
        /// <param name="columns">The columns that conditions can use, in their display order.</param>
        /// <param name="isColumnHidden">Checks if a column is hidden in the table, or null if no column is hidden.</param>
        public void SetFilter(FilterGroup root, IReadOnlyList<TableViewColumn> columns, Predicate<string> isColumnHidden)
        {
            _root = root ?? new FilterGroup();
            _columns = columns ?? Array.Empty<TableViewColumn>();
            _isColumnHidden = isColumnHidden;
            Rebuild();
        }

        /// <summary>
        /// Creates the elements of the tree again, for example after the tree has been changed from outside of the editor.
        /// </summary>
        public void Rebuild()
        {
            Clear();
            Add(CreateGroupElement(_root, null));
        }

        #endregion


        #region Groups

        /// <summary>
        /// Creates the element of a group: its operator, its nodes, and the buttons that add nodes to it.
        /// </summary>
        /// <param name="group">The group to edit.</param>
        /// <param name="parent">The group that contains the group, or null for the root, which can't be removed.</param>
        /// <returns>Returns the created element.</returns>
        private VisualElement CreateGroupElement(FilterGroup group, FilterGroup parent)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(GroupClass);
            element.EnableInClassList(RootGroupClass, parent == null);

            VisualElement header = new VisualElement();
            header.AddToClassList(GroupHeaderClass);
            DropdownField operatorField = new DropdownField
            {
                choices = OperatorChoices,
                index = group.Operator == FilterOperator.Or ? 1 : 0,
                tooltip = OperatorTooltip,
            };
            operatorField.AddToClassList(GroupOperatorClass);
            operatorField.RegisterValueChangedCallback(evt =>
            {
                group.Operator = operatorField.index == 1 ? FilterOperator.Or : FilterOperator.And;
                NotifyValueChanged();
            });
            header.Add(operatorField);

            if (parent != null)
                header.Add(CreateRemoveButton(RemoveGroupTooltip, () => RemoveNode(parent, group)));

            element.Add(header);

            VisualElement nodes = new VisualElement();
            nodes.AddToClassList(GroupNodesClass);
            bool hasNodes = false;
            foreach (FilterNode node in group.Nodes)
            {
                if (node is FilterGroup subgroup)
                    nodes.Add(CreateGroupElement(subgroup, group));
                else if (node is FilterCondition condition)
                    nodes.Add(CreateConditionElement(condition, group));
                else
                    continue;

                hasNodes = true;
            }

            if (!hasNodes)
            {
                Label emptyLabel = new Label(parent == null ? EmptyRootText : EmptyGroupText);
                emptyLabel.AddToClassList(GroupEmptyClass);
                nodes.Add(emptyLabel);
            }

            element.Add(nodes);

            VisualElement footer = new VisualElement();
            footer.AddToClassList(GroupFooterClass);
            Button addConditionButton = null;
            addConditionButton = CreateAddButton(DatabaseIcons.AddCondition, AddConditionText, AddConditionTooltip, () =>
                ShowColumnMenu(addConditionButton, column => AddCondition(group, column)));
            addConditionButton.SetEnabled(_columns.Count > 0);
            footer.Add(addConditionButton);
            footer.Add(CreateAddButton(DatabaseIcons.AddGroup, AddGroupText, AddGroupTooltip, () => AddGroup(group)));
            element.Add(footer);

            return element;
        }

        /// <summary>
        /// Adds the default condition of a column to a group (see <see cref="FilterConditions.CreateDefault"/>).
        /// </summary>
        /// <param name="group">The group to which the condition is added.</param>
        /// <param name="column">The column of the condition.</param>
        private void AddCondition(FilterGroup group, TableViewColumn column)
        {
            group.Nodes.Add(FilterConditions.CreateDefault(column.Id, column.Accessor.ValueType));
            NotifyStructureChanged();
        }

        /// <summary>
        /// Adds an empty group to a group, with the other operator, since a group with the same operator as its parent would be the
        /// same as adding its conditions to the parent.
        /// </summary>
        /// <param name="group">The group to which the new group is added.</param>
        private void AddGroup(FilterGroup group)
        {
            FilterOperator filterOperator = group.Operator == FilterOperator.And ? FilterOperator.Or : FilterOperator.And;
            group.Nodes.Add(new FilterGroup(filterOperator));
            NotifyStructureChanged();
        }

        /// <summary>
        /// Removes a node from its group.
        /// </summary>
        /// <param name="parent">The group that contains the node.</param>
        /// <param name="node">The node to remove.</param>
        private void RemoveNode(FilterGroup parent, FilterNode node)
        {
            parent.Nodes.Remove(node);
            NotifyStructureChanged();
        }

        #endregion


        #region Conditions

        /// <summary>
        /// Creates the element of a condition: its column, the fields of its values, and the button that removes it.
        /// </summary>
        /// <param name="condition">The condition to edit.</param>
        /// <param name="parent">The group that contains the condition.</param>
        /// <returns>Returns the created element.</returns>
        private VisualElement CreateConditionElement(FilterCondition condition, FilterGroup parent)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(ConditionClass);

            TableViewColumn column = FindColumn(condition.ColumnId);
            Button columnButton = null;
            columnButton = new Button(() => ShowColumnMenu(columnButton, newColumn => ChangeColumn(parent, condition, newColumn)))
            {
                text = column != null ? column.Name : $"Missing ({condition.ColumnId})",
                tooltip = ColumnTooltip,
            };
            columnButton.AddToClassList(ConditionColumnClass);
            columnButton.EnableInClassList(ConditionColumnMissingClass, column == null);
            element.Add(columnButton);

            Type valueType = column?.Accessor.ValueType;
            bool isApplicable = valueType != null && condition.CanApplyTo(valueType);
            VisualElement fields = isApplicable ? CreateConditionFields(condition) : CreateNote(IgnoredText);
            element.Add(fields);

            element.Add(CreateRemoveButton(RemoveConditionTooltip, () => RemoveNode(parent, condition)));
            return element;
        }

        /// <summary>
        /// Creates the fields that edit the values of a condition, by kind of condition.
        /// </summary>
        /// <param name="condition">The condition to edit, which applies to its column.</param>
        /// <returns>Returns the created fields.</returns>
        private VisualElement CreateConditionFields(FilterCondition condition)
        {
            // @todo 21b2b: the fields of the number, enum and reference conditions
            switch (condition)
            {
                case TextCondition textCondition:
                    return CreateTextFields(textCondition);

                case BoolCondition boolCondition:
                    return CreateBoolFields(boolCondition);

                default:
                    return CreateNote(NotEditableText);
            }
        }

        /// <summary>
        /// Creates the field of a text condition: the text the cells must contain.
        /// </summary>
        /// <param name="condition">The condition to edit.</param>
        /// <returns>Returns the created field.</returns>
        private VisualElement CreateTextFields(TextCondition condition)
        {
            // Not delayed: the rows are filtered while the user types, like the search
            TextField field = new TextField { value = condition.Text, tooltip = TextTooltip };
            field.textEdition.placeholder = TextPlaceholder;
            field.AddToClassList(ConditionFieldClass);
            field.RegisterValueChangedCallback(evt =>
            {
                condition.Text = evt.newValue;
                NotifyValueChanged();
            });
            return field;
        }

        /// <summary>
        /// Creates the field of a checkbox condition: checked or unchecked.
        /// </summary>
        /// <param name="condition">The condition to edit.</param>
        /// <returns>Returns the created field.</returns>
        private VisualElement CreateBoolFields(BoolCondition condition)
        {
            DropdownField field = new DropdownField
            {
                choices = CheckedChoices,
                index = condition.IsChecked ? 0 : 1,
            };
            field.AddToClassList(ConditionFieldClass);
            field.RegisterValueChangedCallback(evt =>
            {
                condition.IsChecked = field.index == 0;
                NotifyValueChanged();
            });
            return field;
        }

        /// <summary>
        /// Gives a condition another column. The condition is kept if it applies to the values of the new column, and replaced with
        /// the default condition of the column otherwise (see <see cref="FilterConditions.CreateDefault"/>).
        /// </summary>
        /// <param name="parent">The group that contains the condition.</param>
        /// <param name="condition">The condition of which to change the column.</param>
        /// <param name="column">The new column of the condition.</param>
        private void ChangeColumn(FilterGroup parent, FilterCondition condition, TableViewColumn column)
        {
            Type valueType = column.Accessor.ValueType;
            if (condition.CanApplyTo(valueType))
            {
                condition.ColumnId = column.Id;
            }
            else
            {
                int index = parent.Nodes.IndexOf(condition);
                if (index < 0)
                    return;

                parent.Nodes[index] = FilterConditions.CreateDefault(column.Id, valueType);
            }

            NotifyStructureChanged();
        }

        #endregion


        #region Private API

        /// <summary>
        /// Shows the menu of the columns under an element.
        /// </summary>
        /// <param name="anchor">The element under which the menu is shown.</param>
        /// <param name="onPicked">Called with the column picked by the user.</param>
        private void ShowColumnMenu(VisualElement anchor, Action<TableViewColumn> onPicked)
        {
            GenericMenu menu = new GenericMenu();
            foreach (TableViewColumn column in _columns)
            {
                TableViewColumn pickedColumn = column;
                bool isHidden = _isColumnHidden != null && _isColumnHidden(column.Id);
                string text = isHidden ? $"{column.Name}{HiddenColumnSuffix}" : column.Name;
                menu.AddItem(new GUIContent(text), false, () => onPicked(pickedColumn));
            }

            menu.DropDown(anchor.worldBound);
        }

        /// <summary>
        /// Finds a column that conditions can use.
        /// </summary>
        /// <param name="columnId">The identifier of the column.</param>
        /// <returns>Returns the column, or null if the table has no such column.</returns>
        private TableViewColumn FindColumn(string columnId)
        {
            foreach (TableViewColumn column in _columns)
            {
                if (column.Id == columnId)
                    return column;
            }

            return null;
        }

        /// <summary>
        /// Creates a button with an icon and a label, that adds a node to a group.
        /// </summary>
        /// <param name="iconName">The name of the icon, one of the constants of <see cref="DatabaseIcons"/>.</param>
        /// <param name="label">The label of the button.</param>
        /// <param name="tooltip">The tooltip of the button.</param>
        /// <param name="onClicked">Called when the button is clicked.</param>
        /// <returns>Returns the created button.</returns>
        private static Button CreateAddButton(string iconName, string label, string tooltip, Action onClicked)
        {
            Button button = new Button(onClicked) { tooltip = tooltip };
            DatabaseElements.SetIcon(button, iconName, label);
            button.AddToClassList(AddButtonClass);
            return button;
        }

        /// <summary>
        /// Creates a button with a cross icon, that removes an item.
        /// </summary>
        /// <param name="tooltip">The tooltip of the button.</param>
        /// <param name="onClicked">Called when the button is clicked.</param>
        /// <returns>Returns the created button.</returns>
        private static Button CreateRemoveButton(string tooltip, Action onClicked)
        {
            Button button = new Button(onClicked) { tooltip = tooltip };
            DatabaseElements.SetIcon(button, DatabaseIcons.Remove);
            button.AddToClassList(RemoveButtonClass);
            return button;
        }

        /// <summary>
        /// Creates a faded text in the place of the fields of a condition.
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <returns>Returns the created label.</returns>
        private static Label CreateNote(string text)
        {
            Label label = new Label(text);
            label.AddToClassList(ConditionIgnoredClass);
            return label;
        }

        /// <summary>
        /// Notifies a change of a value of the tree, which doesn't change its elements.
        /// </summary>
        private void NotifyValueChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// Notifies a change of the structure of the tree, after building its elements again.
        /// </summary>
        private void NotifyStructureChanged()
        {
            Rebuild();
            Changed?.Invoke();
        }

        #endregion

    }

}
