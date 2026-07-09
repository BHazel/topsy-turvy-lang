import SwiftUI

/// Configuration of command-line arguments (`THE PROPS`) and Standard Input.
struct RunInputsView: View {
    /// The dismiss action, from the environment.
    @Environment(\.dismiss) private var dismiss

    /// The command-line arguments.
    @Binding var commandLineArguments: [String]

    /// The standard input lines.
    @Binding var stdinLines: [String]

    /// The view body.
    var body: some View {
        NavigationStack {
            List {
                Section("Arguments") {
                    ForEach(self.commandLineArguments.indices, id: \.self) { index in
                        TextField("Argument", text: self.$commandLineArguments[index])
                            .swipeActions {
                                Button(role: .destructive) {
                                    self.commandLineArguments.remove(at: index)
                                } label: {
                                    Image(systemName: "trash")
                                }
                            }
                    }

                    Button {
                        self.commandLineArguments.append("")
                    } label: {
                        Label("Add Command-Line Argument", systemImage: "plus")
                    }
                }

                Section("Standard Input") {
                    ForEach(self.stdinLines.indices, id: \.self) { index in
                        TextField("Input Line", text: self.$stdinLines[index])
                            .swipeActions {
                                Button(role: .destructive) {
                                    self.stdinLines.remove(at: index)
                                } label: {
                                    Image(systemName: "trash")
                                }
                            }
                    }

                    Button {
                        self.stdinLines.append("")
                    } label: {
                        Label("Add Standard Input Line", systemImage: "plus")
                    }
                }
            }
            .listStyle(.insetGrouped)
            .navigationTitle("Arguments & Input")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Close") {
                        dismiss()
                    }
                }
            }
        }
    }
}
