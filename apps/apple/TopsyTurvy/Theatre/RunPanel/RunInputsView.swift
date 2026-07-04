import SwiftUI

/// Arguments (`THE PROPS`) and STDIN, shown on demand rather than permanently.
///
/// Each platform gets its own native idiom: macOS uses a grouped `Form` (the System Settings look) with a
/// borderless minus button per row — swipe-to-delete's red Delete button is an iOS gesture and reads as
/// foreign in a Mac popover. iOS uses an inset-grouped `List` with a swipe action (an explicit icon-only
/// `Button(role: .destructive)`, not the default `.onDelete`, since the default swipe action's "Delete" text
/// label doesn't fit well against a bare trash icon at this row size), presented as a medium-detent sheet by
/// the caller.
struct RunInputsView: View {
    @Binding var args: [String]
    @Binding var stdinLines: [String]

    var body: some View {
        #if os(macOS)
        Form {
            Section("Arguments") {
                ForEach(args.indices, id: \.self) { index in
                    HStack {
                        TextField("Argument \(index + 1)", text: $args[index])
                            .textFieldStyle(.plain)
                        Button {
                            args.remove(at: index)
                        } label: {
                            Image(systemName: "minus.circle.fill")
                                .foregroundStyle(.secondary)
                        }
                        .buttonStyle(.borderless)
                    }
                }
                Button {
                    args.append("")
                } label: {
                    Label("Add Argument", systemImage: "plus")
                }
            }

            Section("Standard Input") {
                ForEach(stdinLines.indices, id: \.self) { index in
                    HStack {
                        TextField("Input Line \(index + 1)", text: $stdinLines[index])
                            .textFieldStyle(.plain)
                        Button {
                            stdinLines.remove(at: index)
                        } label: {
                            Image(systemName: "minus.circle.fill")
                                .foregroundStyle(.secondary)
                        }
                        .buttonStyle(.borderless)
                    }
                }
                Button {
                    stdinLines.append("")
                } label: {
                    Label("Add Input Line", systemImage: "plus")
                }
            }
        }
        .formStyle(.grouped)
        .frame(width: 340, height: 400)
        #else
        NavigationStack {
            List {
                Section("Arguments") {
                    ForEach(args.indices, id: \.self) { index in
                        TextField("Argument", text: $args[index])
                            .swipeActions {
                                Button(role: .destructive) {
                                    args.remove(at: index)
                                } label: {
                                    Image(systemName: "trash")
                                }
                            }
                    }

                    Button {
                        args.append("")
                    } label: {
                        Label("Add Argument", systemImage: "plus")
                    }
                }

                Section("Standard Input") {
                    ForEach(stdinLines.indices, id: \.self) { index in
                        TextField("Input Line", text: $stdinLines[index])
                            .swipeActions {
                                Button(role: .destructive) {
                                    stdinLines.remove(at: index)
                                } label: {
                                    Image(systemName: "trash")
                                }
                            }
                    }

                    Button {
                        stdinLines.append("")
                    } label: {
                        Label("Add Input Line", systemImage: "plus")
                    }
                }
            }
            .listStyle(.insetGrouped)
            .navigationTitle("Arguments & Input")
            .navigationBarTitleDisplayMode(.inline)
        }
        #endif
    }
}
